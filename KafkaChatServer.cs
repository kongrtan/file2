// File: Program.cs
// .NET 8 minimal WebSocket + Kafka chat server (Confluent.Kafka)
// Features:
// - WebSocket endpoint: /ws?roomId=<room>&userId=<user>
// - Produces messages to Kafka topic (key=roomId) and consumes via a consumer group
// - Broadcasts consumed messages to all WebSocket clients in the same room
// - Ensures topic exists on startup via AdminClient (configurable partitions/retention)
// - SASL/SSL ready via configuration

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;

var builder = WebApplication.CreateBuilder(args);

// ====== Configuration ======
// You can move these to appsettings.json or environment variables.
var kafkaConfig = new KafkaSettings
{
    BootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP") ?? "broker1:9092",
    Topic = Environment.GetEnvironmentVariable("KAFKA_TOPIC") ?? "chat-messages",
    Partitions = int.TryParse(Environment.GetEnvironmentVariable("KAFKA_PARTITIONS"), out var p) ? p : 12,
    ReplicationFactor = short.TryParse(Environment.GetEnvironmentVariable("KAFKA_RF"), out var rf) ? rf : (short)1,
    ConsumerGroupId = Environment.GetEnvironmentVariable("KAFKA_GROUP") ?? "chat-server",
    // SASL/SSL (optional)
    SecurityProtocol = Environment.GetEnvironmentVariable("KAFKA_SECURITY_PROTOCOL"),
    SaslMechanism = Environment.GetEnvironmentVariable("KAFKA_SASL_MECHANISM"),
    SaslUsername = Environment.GetEnvironmentVariable("KAFKA_USERNAME"),
    SaslPassword = Environment.GetEnvironmentVariable("KAFKA_PASSWORD"),
    SslCaLocation = Environment.GetEnvironmentVariable("KAFKA_SSL_CA"),
};

builder.Services.AddSingleton(kafkaConfig);

// Shared connection registry per room
builder.Services.AddSingleton(new RoomHub());

// Producer (singleton)
builder.Services.AddSingleton<IProducer<string, string>>(sp =>
{
    var cfg = BuildProducerConfig(sp.GetRequiredService<KafkaSettings>());
    return new ProducerBuilder<string, string>(cfg)
        .SetErrorHandler((_, e) => Console.WriteLine($"[ProducerError] {e.Reason}"))
        .Build();
});

// Background Kafka consumer hosted service
builder.Services.AddHostedService<KafkaConsumerService>();

// Ensure topic exists at startup
builder.Services.AddHostedService<TopicEnsureService>();

var app = builder.Build();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(20)
});

// Health
app.MapGet("/health", () => Results.Ok("ok"));

// WebSocket endpoint: /ws?roomId=<room>&userId=<user>
app.Map("/ws", async (HttpContext ctx, RoomHub hub, IProducer<string, string> producer, KafkaSettings settings) =>
{
    if (!ctx.WebSockets.IsWebSocketRequest)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        await ctx.Response.WriteAsync("WebSocket required");
        return;
    }

    var roomId = ctx.Request.Query["roomId"].ToString();
    var userId = ctx.Request.Query["userId"].ToString();

    if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(userId))
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        await ctx.Response.WriteAsync("roomId and userId required");
        return;
    }

    using var socket = await ctx.WebSockets.AcceptWebSocketAsync();
    var connection = new ClientConnection(socket, roomId, userId);
    hub.Add(connection);
    Console.WriteLine($"[WS] Connected: {userId} -> room {roomId}");

    // Notify join (optional)
    await SendKafka(producer, settings.Topic, roomId, new ChatMessage
    {
        RoomId = roomId,
        UserId = "system",
        Type = "join",
        Text = $"{userId} joined",
        Ts = DateTimeOffset.UtcNow
    });

    var recvBuffer = new byte[64 * 1024];

    try
    {
        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(recvBuffer, CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }

            var msgText = Encoding.UTF8.GetString(recvBuffer, 0, result.Count);
            ChatMessage payload;
            try
            {
                // Accept either raw text or JSON ChatMessage
                payload = JsonSerializer.Deserialize<ChatMessage>(msgText) ?? new ChatMessage { Text = msgText };
            }
            catch
            {
                payload = new ChatMessage { Text = msgText };
            }

            payload.RoomId = roomId;
            payload.UserId = userId;
            payload.Type = string.IsNullOrEmpty(payload.Type) ? "chat" : payload.Type;
            payload.Ts = DateTimeOffset.UtcNow;

            await SendKafka(producer, settings.Topic, roomId, payload);
        }
    }
    finally
    {
        hub.Remove(connection);
        Console.WriteLine($"[WS] Disconnected: {userId} -> room {roomId}");
        await SendKafka(producer, settings.Topic, roomId, new ChatMessage
        {
            RoomId = roomId,
            UserId = "system",
            Type = "leave",
            Text = $"{userId} left",
            Ts = DateTimeOffset.UtcNow
        });
    }
});

app.Run();

// ====== Types & Services ======

record KafkaSettings
{
    public string BootstrapServers { get; init; } = default!;
    public string Topic { get; init; } = "chat-messages";
    public int Partitions { get; init; } = 12;
    public short ReplicationFactor { get; init; } = 1;
    public string ConsumerGroupId { get; init; } = "chat-server";

    // Security (optional)
    public string? SecurityProtocol { get; init; }
    public string? SaslMechanism { get; init; }
    public string? SaslUsername { get; init; }
    public string? SaslPassword { get; init; }
    public string? SslCaLocation { get; init; }
}

class RoomHub
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<ClientConnection, byte>> _rooms = new();

    public void Add(ClientConnection conn)
    {
        var set = _rooms.GetOrAdd(conn.RoomId, _ => new ConcurrentDictionary<ClientConnection, byte>());
        set[conn] = 0;
    }

    public void Remove(ClientConnection conn)
    {
        if (_rooms.TryGetValue(conn.RoomId, out var set))
        {
            set.TryRemove(conn, out _);
            if (set.IsEmpty)
                _rooms.TryRemove(conn.RoomId, out _);
        }
    }

    public IEnumerable<ClientConnection> GetRoom(string roomId)
        => _rooms.TryGetValue(roomId, out var set) ? set.Keys : Enumerable.Empty<ClientConnection>();
}

record ClientConnection(WebSocket Socket, string RoomId, string UserId);

record ChatMessage
{
    public string RoomId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string Type { get; set; } = "chat"; // chat | join | leave | system
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset Ts { get; set; } = DateTimeOffset.UtcNow;
}

static ProducerConfig BuildProducerConfig(KafkaSettings s)
{
    var cfg = new ProducerConfig
    {
        BootstrapServers = s.BootstrapServers,
        Acks = Acks.All,
        EnableIdempotence = true,
        // linger/batch can be tuned
    };

    // Security
    if (!string.IsNullOrEmpty(s.SecurityProtocol))
        cfg.SecurityProtocol = Enum.Parse<SecurityProtocol>(s.SecurityProtocol, ignoreCase: true);
    if (!string.IsNullOrEmpty(s.SaslMechanism))
        cfg.SaslMechanism = Enum.Parse<SaslMechanism>(s.SaslMechanism, ignoreCase: true);
    if (!string.IsNullOrEmpty(s.SaslUsername)) cfg.SaslUsername = s.SaslUsername;
    if (!string.IsNullOrEmpty(s.SaslPassword)) cfg.SaslPassword = s.SaslPassword;
    if (!string.IsNullOrEmpty(s.SslCaLocation)) cfg.SslCaLocation = s.SslCaLocation;
    return cfg;
}

static ConsumerConfig BuildConsumerConfig(KafkaSettings s)
{
    var cfg = new ConsumerConfig
    {
        BootstrapServers = s.BootstrapServers,
        GroupId = s.ConsumerGroupId,
        EnableAutoCommit = false, // manual commit for at-least-once
        AutoOffsetReset = AutoOffsetReset.Earliest,
        PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky
    };

    // Security
    if (!string.IsNullOrEmpty(s.SecurityProtocol))
        cfg.SecurityProtocol = Enum.Parse<SecurityProtocol>(s.SecurityProtocol, ignoreCase: true);
    if (!string.IsNullOrEmpty(s.SaslMechanism))
        cfg.SaslMechanism = Enum.Parse<SaslMechanism>(s.SaslMechanism, ignoreCase: true);
    if (!string.IsNullOrEmpty(s.SaslUsername)) cfg.SaslUsername = s.SaslUsername;
    if (!string.IsNullOrEmpty(s.SaslPassword)) cfg.SaslPassword = s.SaslPassword;
    if (!string.IsNullOrEmpty(s.SslCaLocation)) cfg.SslCaLocation = s.SslCaLocation;
    return cfg;
}

static async Task SendKafka(IProducer<string, string> producer, string topic, string roomId, ChatMessage msg)
{
    var json = JsonSerializer.Serialize(msg);
    await producer.ProduceAsync(topic, new Message<string, string>
    {
        Key = roomId, // ensures per-room ordering when using key_partitioning
        Value = json
    });
}

// Background consumer that reads Kafka and fan-outs to WebSocket clients
class KafkaConsumerService : BackgroundService
{
    private readonly KafkaSettings _settings;
    private readonly RoomHub _hub;

    public KafkaConsumerService(KafkaSettings settings, RoomHub hub)
    {
        _settings = settings;
        _hub = hub;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new ConsumerBuilder<string, string>(BuildConsumerConfig(_settings))
            .SetErrorHandler((_, e) => Console.WriteLine($"[ConsumerError] {e.Reason}"))
            .SetPartitionsAssignedHandler((c, parts) =>
            {
                Console.WriteLine($"[Rebalance] Assigned {string.Join(",", parts)}");
            })
            .SetPartitionsRevokedHandler((c, parts) =>
            {
                Console.WriteLine($"[Rebalance] Revoked {string.Join(",", parts)}");
            })
            .Build();

        consumer.Subscribe(_settings.Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var cr = consumer.Consume(TimeSpan.FromMilliseconds(250));
                    if (cr == null) continue;

                    // Fan-out to WebSocket clients in the room
                    var roomId = cr.Message.Key ?? string.Empty;
                    var payload = cr.Message.Value ?? string.Empty;

                    var tasks = _hub.GetRoom(roomId)
                        .Select(conn => SafeSend(conn, payload, stoppingToken))
                        .ToList();

                    await Task.WhenAll(tasks);

                    consumer.Commit(cr); // manual commit after fan-out
                }
                catch (ConsumeException ex)
                {
                    Console.WriteLine($"[ConsumeException] {ex.Error.Reason}");
                }
            }
        }
        finally
        {
            consumer.Close();
            consumer.Dispose();
        }
    }

    private static async Task SafeSend(ClientConnection conn, string text, CancellationToken ct)
    {
        if (conn.Socket.State != WebSocketState.Open) return;
        var buffer = Encoding.UTF8.GetBytes(text);
        try
        {
            await conn.Socket.SendAsync(buffer, WebSocketMessageType.Text, true, ct);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[WS send error] {e.Message}");
        }
    }
}

// Ensure topic exists on startup
class TopicEnsureService : IHostedService
{
    private readonly KafkaSettings _settings;

    public TopicEnsureService(KafkaSettings settings)
    {
        _settings = settings;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var adminCfg = new AdminClientConfig
        {
            BootstrapServers = _settings.BootstrapServers
        };
        if (!string.IsNullOrEmpty(_settings.SecurityProtocol))
            adminCfg.SecurityProtocol = Enum.Parse<SecurityProtocol>(_settings.SecurityProtocol, true);
        if (!string.IsNullOrEmpty(_settings.SaslMechanism))
            adminCfg.SaslMechanism = Enum.Parse<SaslMechanism>(_settings.SaslMechanism, true);
        if (!string.IsNullOrEmpty(_settings.SaslUsername)) adminCfg.SaslUsername = _settings.SaslUsername;
        if (!string.IsNullOrEmpty(_settings.SaslPassword)) adminCfg.SaslPassword = _settings.SaslPassword;
        if (!string.IsNullOrEmpty(_settings.SslCaLocation)) adminCfg.SslCaLocation = _settings.SslCaLocation;

        using var admin = new AdminClientBuilder(adminCfg).Build();
        try
        {
            var md = admin.GetMetadata(TimeSpan.FromSeconds(5));
            if (md.Topics.Any(t => t.Topic == _settings.Topic))
            {
                Console.WriteLine($"[Topic] '{_settings.Topic}' exists");
                return;
            }

            Console.WriteLine($"[Topic] Creating '{_settings.Topic}'...");
            await admin.CreateTopicsAsync(new[]
            {
                new TopicSpecification
                {
                    Name = _settings.Topic,
                    NumPartitions = _settings.Partitions,
                    ReplicationFactor = _settings.ReplicationFactor,
                    // Optional: compaction/retention
                    Configs = new Dictionary<string, string>
                    {
                        ["cleanup.policy"] = "delete",
                        ["retention.ms"] = (3 * 24 * 60 * 60 * 1000L).ToString() // 3 days
                    }
                }
            });
            Console.WriteLine($"[Topic] Created '{_settings.Topic}'");
        }
        catch (CreateTopicsException e)
        {
            if (e.Results.Any(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
                Console.WriteLine($"[Topic] Already exists: {_settings.Topic}");
            else
                Console.WriteLine($"[Topic] Create failed: {e.Message}");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
