
# RV NetTransport 동적 서버 관리 샘플

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

## 3. NetTransportManager (동적 서버 관리)

```csharp
using System.Collections.Concurrent;

public class NetTransportManager : IDisposable
{
    private readonly ConcurrentDictionary<string, NetTransportService> _transports = new();

    public bool RegisterServer(string serverId)
    {
        return _transports.TryAdd(serverId, new NetTransportService());
    }

    public bool UnregisterServer(string serverId)
    {
        if (_transports.TryRemove(serverId, out var service))
        {
            service.Dispose();
            return true;
        }
        return false;
    }

    public INetTransportService? GetTransport(string serverId)
    {
        _transports.TryGetValue(serverId, out var service);
        return service;
    }

    public void Dispose()
    {
        foreach (var kv in _transports)
        {
            kv.Value.Dispose();
        }
        _transports.Clear();
    }
}
```

## 4. RVController (API)

```csharp
[ApiController]
[Route("api/rv")]
public class RVController : ControllerBase
{
    private readonly NetTransportManager _manager;

    public RVController(NetTransportManager manager)
    {
        _manager = manager;
    }

    // 메시지 전송
    [HttpPost("send/{serverId}")]
    public async Task<IActionResult> Send(string serverId, [FromBody] string message)
    {
        var transport = _manager.GetTransport(serverId);
        if (transport == null)
            return NotFound($"서버 {serverId} 등록되지 않음");

        await transport.SendAsync(message);
        return Ok($"메시지 큐에 추가됨 (서버 {serverId})");
    }

    // 서버 등록
    [HttpPost("register/{serverId}")]
    public IActionResult Register(string serverId)
    {
        if (_manager.RegisterServer(serverId))
            return Ok($"서버 {serverId} 등록 완료");
        return BadRequest($"서버 {serverId} 이미 등록됨");
    }

    // 서버 삭제
    [HttpDelete("unregister/{serverId}")]
    public IActionResult Unregister(string serverId)
    {
        if (_manager.UnregisterServer(serverId))
            return Ok($"서버 {serverId} 제거 완료");
        return NotFound($"서버 {serverId} 없음");
    }
}
```

## 5. Program.cs (DI 등록)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<NetTransportManager>();
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

## 6. 특징

1. 동적 서버 관리 (등록/삭제) 가능, 최대 100대 이상 확장 가능
2. 멀티스레드 안전 + 큐 기반 비동기 전송
3. 연결 끊김 시 재연결/재시도
4. 관리 화면/Controller에서 쉽게 메시지 전송 가능
5. Dispose 시 모든 서버 NetTransport 안전 종료
