# Dapr + Kafka Request/Response 아키텍처 논의

## 🧩 전체 아키텍처 요약

```
[Client] 
   ↓ (HTTP REST)
[API Controller]  ←→  [daprd Sidecar]
   ↓ (Pub via daprd)
[Kafka Topic: request]
   ↓
[A 시스템 (레거시)]  ←→  [Kafka Topic: response]
   ↑
[daprd Subscribed Controller]
   ↑
(응답을 기다리던 Controller가 응답을 반환)
   ↑
[Client]
```

---

## ⚙️ 핵심 개념 요약

| 요소 | 역할 |
|------|------|
| **Client** | 단순히 REST API 호출 |
| **ASP.NET Controller** | Request/Response 담당, Kafka 메시지 Pub/Sub 관제 역할 |
| **daprd** | Dapr Sidecar — Kafka Pub/Sub를 추상화 |
| **Kafka** | 비동기 메시지 브로커 (Request/Response 연결 매개체) |
| **A 시스템** | Kafka 메시지 처리 후 응답 메시지 발행 (레거시 시스템) |

---

## 🧠 작동 시나리오 상세

1. **Client → Controller**
   - 클라이언트가 `/api/send` REST API를 호출
   - Controller가 요청 데이터를 수신

2. **Controller → daprd (Kafka Publish)**
   - Controller는 `daprd`에 HTTP POST로 `/v1.0/publish/kafka-pubsub/request-topic` 호출  
   - 메시지에는 `correlationId` 포함  

3. **Controller → 응답 대기**
   - `TaskCompletionSource` 기반으로 `correlationId` 대기
   - 예: 5초 타임아웃 설정

4. **A 시스템 → Kafka 응답 발행**
   - `response-topic`에 `correlationId` 포함하여 응답

5. **daprd → Controller**
   - `[Topic("kafka-pubsub", "response-topic")]` 엔드포인트 호출
   - 응답을 매칭 후 반환

---

## 📄 구현 예시 (ASP.NET Core + Dapr)

### `ResponseWaiter.cs`
```csharp
using System.Collections.Concurrent;

public class ResponseWaiter
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pending = new();

    public Task<string> WaitForResponseAsync(string correlationId, TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[correlationId] = tcs;

        var cts = new CancellationTokenSource(timeout);
        cts.Token.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false);

        return tcs.Task;
    }

    public void SetResponse(string correlationId, string payload)
    {
        if (_pending.TryRemove(correlationId, out var tcs))
            tcs.TrySetResult(payload);
    }
}
```

### `SendController.cs`
```csharp
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class SendController : ControllerBase
{
    private readonly DaprClient _daprClient;
    private readonly ResponseWaiter _responseWaiter;

    public SendController(DaprClient daprClient, ResponseWaiter responseWaiter)
    {
        _daprClient = daprClient;
        _responseWaiter = responseWaiter;
    }

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] RequestDto req)
    {
        string correlationId = Guid.NewGuid().ToString();

        var message = new
        {
            CorrelationId = correlationId,
            Payload = req
        };

        await _daprClient.PublishEventAsync("kafka-pubsub", "request-topic", message);

        try
        {
            var response = await _responseWaiter.WaitForResponseAsync(correlationId, TimeSpan.FromSeconds(5));
            return Ok(new { correlationId, response });
        }
        catch (TaskCanceledException)
        {
            return StatusCode(504, "Response timeout");
        }
    }
}

public record RequestDto(string Action, string Data);
```

### `ResponseController.cs`
```csharp
using Dapr;
using Microsoft.AspNetCore.Mvc;

[ApiController]
public class ResponseController : ControllerBase
{
    private readonly ResponseWaiter _responseWaiter;

    public ResponseController(ResponseWaiter responseWaiter)
    {
        _responseWaiter = responseWaiter;
    }

    [Topic("kafka-pubsub", "response-topic")]
    [HttpPost("onresponse")]
    public IActionResult OnResponse([FromBody] ResponseDto res)
    {
        _responseWaiter.SetResponse(res.CorrelationId, res.Payload);
        return Ok();
    }
}

public record ResponseDto(string CorrelationId, string Payload);
```

### `Program.cs`
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDaprClient();
builder.Services.AddSingleton<ResponseWaiter>();

var app = builder.Build();

app.UseCloudEvents();
app.MapControllers();
app.MapSubscribeHandler();

app.Run();
```

---

## ⚠️ 주의점

| 항목 | 설명 |
|------|------|
| **Controller는 요청마다 생성됨** | 맞음. 그래서 전역 서비스(ResponseWaiter)에 상태 보관 |
| **스케일아웃 시 주의** | 여러 인스턴스일 경우 Redis 등 외부 스토리지 필요 |
| **메모리 관리** | 오래된 대기 Task는 정리 필요 |

---

## ✅ 결론

- Controller 내부에는 상태를 두지 말고, 싱글톤 서비스로 분리해야 함  
- Kafka 응답은 `[Topic]` 구독 방식으로 처리  
- 단일 인스턴스에서는 완벽히 동작하며, 분산 시 Redis 또는 Dapr State Store로 확장 가능
