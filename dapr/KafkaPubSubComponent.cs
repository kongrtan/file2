using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapr.PluggableComponents;
using Dapr.PluggableComponents.Components.Pubsub;
using Dapr.Proto.Components.V1;

// 필요하다면 Kafka 클라이언트 네임스페이스 추가
using Confluent.Kafka;

public class KafkaPubSubComponent : PubsubBase
{
    private string _bootstrapServers = "";
    private string _topicPrefix = ""; // optional metadata
    private string _consumerGroup = "";
    private KafkaProducer<string, byte[]>? _producer;  // 예시용
    private IConsumer<string, byte[]>? _consumer;

    public override Task InitAsync(MetadataRequest request, CancellationToken cancellationToken = default)
    {
        // metadata 설정값 꺼내기
        if (request.Properties.TryGetValue("bootstrapServers", out var bs))
        {
            _bootstrapServers = bs;
        }
        if (request.Properties.TryGetValue("topicPrefix", out var tp))
        {
            _topicPrefix = tp;
        }
        if (request.Properties.TryGetValue("consumerGroup", out var cg))
        {
            _consumerGroup = cg;
        }

        // Kafka 클라이언트 설정
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _bootstrapServers,
            // 기타 설정 (압축, 직렬화, etc.)
        };
        _producer = new KafkaProducer<string, byte[]>(producerConfig);

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = _consumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest
            // 기타 설정
        };
        _consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build();

        Console.WriteLine($"[KafkaPubSub] Initialized with bootstrapServers={_bootstrapServers}, topicPrefix={_topicPrefix}, consumerGroup={_consumerGroup}");
        return Task.CompletedTask;
    }

    public override async Task PublishAsync(PubSubPublishRequest request, CancellationToken cancellationToken = default)
    {
        if (_producer == null)
        {
            throw new InvalidOperationException("Producer is not initialized");
        }

        var topic = _topicPrefix + request.Topic;
        var messageBytes = request.Data.ToByteArray();  // 또는 적절한 직렬화
        var msg = new Message<string, byte[]> { Key = null, Value = messageBytes };

        // 비동기 전송
        await _producer.ProduceAsync(topic, msg, cancellationToken);
    }

    public override async Task PullMessagesAsync(
        PubSubPullMessagesTopic topic,
        MessageDeliveryHandler<string?, PubSubPullMessagesResponse> deliveryHandler,
        CancellationToken cancellationToken = default)
    {
        if (_consumer == null)
        {
            throw new InvalidOperationException("Consumer is not initialized");
        }

        string actualTopic = _topicPrefix + topic.Topic;

        _consumer.Subscribe(actualTopic);

        Console.WriteLine($"[KafkaPubSub] Subscribed to {actualTopic}");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var cr = _consumer.Consume(cancellationToken);
                if (cr == null) continue;

                var dataBytes = cr.Message.Value;
                var response = new PubSubPullMessagesResponse(topic.Topic)
                {
                    // 데이터 설정 (클라우드 이벤트 envelope 또는 raw 데이터)
                    Entries =
                    {
                        new PubSubMessage
                        {
                            Data = Google.Protobuf.ByteString.CopyFrom(dataBytes),
                            // Metadata 설정 (예: headers 등)
                            Metadata = { { "kafka_partition", cr.Partition.Value.ToString() } }
                        }
                    }
                };

                // 메시지를 Dapr 런타임에 전달
                await deliveryHandler(response, async (err) =>
                {
                    if (string.IsNullOrEmpty(err))
                    {
                        // 성공 처리 (예: 커밋)
                        _consumer.Commit(cr);
                    }
                    else
                    {
                        // 실패 처리 로직 (재시도, 로그 등)
                        Console.WriteLine($"[KafkaPubSub] Delivery error: {err}");
                    }
                });
            }
        }
        catch (OperationCanceledException)
        {
            // 종료 요청
        }
        finally
        {
            _consumer.Close();
        }
    }
}

