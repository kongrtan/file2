
# RV NetTransport 싱글톤 서비스 구조 정리

## 개요
- RV(Rendezvous) 메시징에서 NetTransport는 **TCP/IP 기반 소켓 연결**
- Controller에서 NetTransport를 매번 초기화하지 않고 **싱글톤 서비스**로 재사용
- 멀티스레드 환경에서도 안전하게 메시지 전송
- 연결 끊김 시 **재연결 및 재시도** 로직 포함

---

## 1. NetTransport 클래스

```csharp
using System;
using System.Threading.Tasks;

public class NetTransport : IDisposable
{
    private bool _initialized;
    private bool _connected;

    public void Initialize()
    {
        if (_initialized) return;
        Console.WriteLine("NetTransport 초기화 완료");
        _initialized = true;
        Connect();
    }

    private void Connect()
    {
        Console.WriteLine("NetTransport 연결 시도...");
        _connected = true;
        Console.WriteLine("NetTransport 연결 성공");
    }

    public async Task SendAsync(string message)
    {
        if (!_connected)
            throw new InvalidOperationException("연결 끊김 상태");

        Console.WriteLine($"[NetTransport] 메시지 전송: {message}");
        await Task.Delay(50);
    }

    public void Disconnect()
    {
        _connected = false;
        Console.WriteLine("NetTransport 연결 끊김");
    }

    public void Reconnect()
    {
        Console.WriteLine("NetTransport 재연결 시도...");
        Connect();
    }

    public void Dispose()
    {
        _connected = false;
        Console.WriteLine("NetTransport 종료");
    }
}
```

---

## 2. NetTransportService (싱글톤 + 큐 + 재연결)

```csharp
using System.Collections.Concurrent;
using System.Threading;

public interface INetTransportService
{
    Task SendAsync(string message);
}

public class NetTransportService : INetTransportService, IDisposable
{
    private readonly NetTransport _netTransport;
    private readonly ConcurrentQueue<string> _sendQueue = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _processingTask;

    public NetTransportService()
    {
        _netTransport = new NetTransport();
        _netTransport.Initialize();
        _processingTask = Task.Run(ProcessQueueAsync);
    }

    public Task SendAsync(string message)
    {
        _sendQueue.Enqueue(message);
        return Task.CompletedTask;
    }

    private async Task ProcessQueueAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            while (_sendQueue.TryDequeue(out var msg))
            {
                bool sent = false;
                int attempt = 0;

                while (!sent && attempt < 3)
                {
                    try
                    {
                        await _netTransport.SendAsync(msg);
                        sent = true;
                    }
                    catch
                    {
                        Console.WriteLine("전송 실패. 재연결 시도...");
                        _netTransport.Reconnect();
                        attempt++;
                        await Task.Delay(100);
                    }
                }

                if (!sent)
                {
                    Console.WriteLine($"메시지 전송 실패: {msg}");
                }
            }

            await Task.Delay(10);
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _processingTask.Wait();
        _netTransport.Dispose();
        _cts.Dispose();
    }
}
```

---

## 3. Controller 예제

```csharp
[ApiController]
[Route("api/rv")]
public class RVController : ControllerBase
{
    private readonly INetTransportService _netTransport;

    public RVController(INetTransportService netTransport)
    {
        _netTransport = netTransport;
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] string message)
    {
        await _netTransport.SendAsync(message);
        return Ok("RV 메시지 큐에 추가됨");
    }
}
```

---

## 4. 특징

1. **싱글톤 재사용**
   - 여러 Controller에서 동일 NetTransport 인스턴스 사용
2. **멀티스레드 안전**
   - ConcurrentQueue + 백그라운드 Task
3. **재연결/재시도**
   - 연결 끊김 시 최대 3회 재시도
4. **비동기 큐잉**
   - send 호출 즉시 큐에 넣고 백그라운드에서 순차 처리
5. **리소스 정리**
   - Dispose 시 Task 종료, NetTransport 종료
