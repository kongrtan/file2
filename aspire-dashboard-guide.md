# Aspire Dashboard 독립형 실행 및 외부 프로젝트 연결 방법

## 🎯 목표

- `Aspire Dashboard`를 독립형으로 실행 (별도 프로세스 또는 Pod)
- 다른 프로젝트(`myproject`)에서 Aspire Dashboard로 메트릭/로그/트레이스를 전송하여 모니터링

---

## 1. Aspire Dashboard 독립 실행

### ✅ CLI 실행

```bash
dotnet aspire dashboard
```

- 기본 포트: `18888`
- OTLP 수신 포트: `18889` (gRPC 기반)

### ✅ Docker 실행 (Kubernetes 사용 시)

```dockerfile
FROM mcr.microsoft.com/dotnet/nightly/aspire-dashboard:8.0
```

> Kubernetes 환경에서는 Service 타입을 `ClusterIP` 또는 `LoadBalancer`로 설정하여 외부 접근을 허용하세요.

---

## 2. `myproject`에서 Dashboard에 Telemetry 전송 설정

### 📦 필수 패키지 설치

```bash
dotnet add package Microsoft.Extensions.Telemetry --prerelease
dotnet add package OpenTelemetry.Exporter.Otlp
dotnet add package OpenTelemetry.Extensions.Hosting
```

또는 메트릭 전용 패키지:

```bash
dotnet add package Microsoft.AspNetCore.Diagnostics.Metrics
```

### ⚙️ `Program.cs` 또는 `Startup.cs`에 설정 추가

```csharp
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;

var builder = WebApplication.CreateBuilder(args);

var otlpEndpoint = "http://<DASHBOARD_HOST>:18889"; // OTLP gRPC endpoint

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddHttpClientInstrumentation();
        metrics.AddRuntimeInstrumentation();
        metrics.AddProcessInstrumentation();
        metrics.AddOtlpExporter(otlp =>
        {
            otlp.Endpoint = new Uri(otlpEndpoint);
        });
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();
        tracing.AddOtlpExporter(otlp =>
        {
            otlp.Endpoint = new Uri(otlpEndpoint);
        });
    });

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.AddOtlpExporter(otlp =>
    {
        otlp.Endpoint = new Uri(otlpEndpoint);
    });
});

var app = builder.Build();
app.MapGet("/", () => "Hello from myproject");
app.Run();
```

---

## 3. 네트워크 및 OTLP 설정 확인

- Aspire Dashboard는 `18889` 포트에서 OTLP (gRPC) 수신
- `myproject`에서 다음 형식으로 Endpoint 설정 필요:
  - 예: `http://aspire-dashboard.default.svc.cluster.local:18889` (Kubernetes DNS)
- 방화벽, 네트워크 정책, 네임스페이스 등을 통해 연결 가능해야 함

---

## 4. Kubernetes 배포 시 주의사항

- `aspire-dashboard`는 별도 Pod + Service로 구성
- `myproject`에서 환경변수나 구성 파일을 통해 OTLP endpoint 지정
- OTLP는 gRPC 기반이므로 **http 대신 grpc 프로토콜을 사용하는 gRPC 클라이언트가 필요함**

---

## ✅ 결과

- `myproject`의 메트릭, 로그, 트레이스를 `Aspire Dashboard`에서 실시간으로 확인 가능
- Dashboard는 여러 프로젝트의 Telemetry를 수집하고 시각화 가능
