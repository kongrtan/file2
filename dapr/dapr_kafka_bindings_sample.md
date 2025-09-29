# Dapr 0.3.0 .NET Kafka Input/Output Binding 샘플

## Kafka Input Binding 구현

### 1. `IInputBinding` 구현
```csharp
using Dapr.PluggableComponents.Components;
using Dapr.PluggableComponents.Components.Bindings;
using Microsoft.Extensions.Logging;

internal sealed class KafkaInputBinding : IInputBinding
{
    private readonly ILogger<KafkaInputBinding> _logger;

    public KafkaInputBinding(ILogger<KafkaInputBinding> logger)
    {
        _logger = logger;
    }

    public Task InitAsync(MetadataRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Kafka Input Binding initialized.");
        return Task.CompletedTask;
    }

    public async Task ReadAsync(MessageDeliveryHandler<InputBindingReadRequest, InputBindingReadResponse> deliveryHandler, CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var message = new InputBindingReadResponse
            {
                Data = new byte[] { /* 메시지 데이터 */ },
                Metadata = new Dictionary<string, string>
                {
                    { "topic", "your-topic" },
                    { "partition", "0" },
                    { "offset", "123" }
                }
            };

            await deliveryHandler(message, async req =>
            {
                _logger.LogInformation("Message processed.");
            });

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }
}
```

### 2. 컴포넌트 등록
```csharp
using Dapr.PluggableComponents;

var app = DaprPluggableComponentsApplication.Create();
app.RegisterService("<소켓 이름>", serviceBuilder =>
{
    serviceBuilder.RegisterBinding<KafkaInputBinding>();
});
app.Run();
```

---

## Kafka Output Binding 구현

### 1. `IOutputBinding` 구현
```csharp
using Dapr.PluggableComponents.Components;
using Dapr.PluggableComponents.Components.Bindings;
using Microsoft.Extensions.Logging;

internal sealed class KafkaOutputBinding : IOutputBinding
{
    private readonly ILogger<KafkaOutputBinding> _logger;

    public KafkaOutputBinding(ILogger<KafkaOutputBinding> logger)
    {
        _logger = logger;
    }

    public Task InitAsync(MetadataRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Kafka Output Binding initialized.");
        return Task.CompletedTask;
    }

    public Task<OutputBindingInvokeResponse> InvokeAsync(OutputBindingInvokeRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Sending message to Kafka topic: {request.Metadata["topic"]}");
        return Task.FromResult(new OutputBindingInvokeResponse());
    }

    public Task<string[]> ListOperationsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new[] { "create" });
    }
}
```

### 2. 컴포넌트 등록
```csharp
using Dapr.PluggableComponents;

var app = DaprPluggableComponentsApplication.Create();
app.RegisterService("<소켓 이름>", serviceBuilder =>
{
    serviceBuilder.RegisterBinding<KafkaOutputBinding>();
});
app.Run();
```

---

## Kafka 컴포넌트 구성 예시
```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: kafka-binding
spec:
  type: bindings.kafka
  version: v1
  metadata:
    - name: brokers
      value: "localhost:9092"
    - name: topics
      value: "input-topic"
    - name: consumerGroup
      value: "group1"
    - name: publishTopic
      value: "output-topic"
```

---

## 참고 자료
- [Dapr 공식 문서 - .NET SDK로 Input/Output Binding 구현](https://docs.dapr.io/developing-applications/develop-components/pluggable-components/pluggable-components-sdks/pluggable-components-dotnet/dotnet-bindings/)
- [Dapr 공식 문서 - Kafka Binding 구성](https://docs.dapr.io/reference/components-reference/supported-bindings/kafka/)
- [Dapr 샘플 리포지토리 - Kafka와의 통합 예시](https://github.com/dapr/samples)

