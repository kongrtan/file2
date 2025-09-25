using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Dapr.PluggableComponents;
using Dapr.PluggableComponents.Components.PubSub;

public class InMemoryPubSub : IPubSub
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<PubSubPublishRequest>> _topics =
        new(StringComparer.OrdinalIgnoreCase);

    public Task InitAsync(MetadataRequest request, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("InMemoryPubSub initialized.");
        return Task.CompletedTask;
    }

    public Task PublishAsync(PubSubPublishRequest request, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[Publish] Topic={request.Topic}, Data={request.Data.Length} bytes");

        var queue = _topics.GetOrAdd(request.Topic, _ => new ConcurrentQueue<PubSubPublishRequest>());
        queue.Enqueue(request);

        return Task.CompletedTask;
    }

    public Task PullMessagesAsync(
        PubSubPullMessagesTopic topic,
        MessageDeliveryHandler<string?, PubSubPullMessagesResponse> deliveryHandler,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(async () =>
        {
            var queue = _topics.GetOrAdd(topic.Name, _ => new ConcurrentQueue<PubSubPublishRequest>());

            while (!cancellationToken.IsCancellationRequested)
            {
                while (queue.TryDequeue(out var msg))
                {
                    var response = new PubSubPullMessagesResponse(topic.Name)
                    {
                        Data = msg.Data,
                        ContentType = msg.ContentType
                    };

                    await deliveryHandler(
                        response,
                        async (errorMessage) =>
                        {
                            if (string.IsNullOrEmpty(errorMessage))
                                Console.WriteLine($"[Ack] {topic.Name}");
                            else
                                Console.WriteLine($"[Nack] {topic.Name}, error={errorMessage}");

                            await Task.CompletedTask;
                        });
                }

                // 잠깐 쉬어주기 (폴링 주기)
                await Task.Delay(500, cancellationToken);
            }
        }, cancellationToken);
    }
}

// Program.cs
var app = DaprPluggableComponentsApplication.Create();

app.RegisterService(
    "inmemory-pubsub",
    serviceBuilder =>
    {
        serviceBuilder.RegisterPubSub<InMemoryPubSub>();
    });

await app.RunAsync();
