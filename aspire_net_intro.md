
# Aspire .NET 소개

## Aspire .NET이란?
- .NET 기반 분산 애플리케이션 구성 플랫폼
- 서비스 연결, 시크릿 관리, 텔레메트리 구성 자동화
- 개발 최적화 대시보드 제공

## Aspire 실행 필수 조건
- .NET 8 SDK 이상
- Docker
- Visual Studio 2022 17.8+ 또는 VS Code
- NuGet 패키지: Aspire.Hosting, Aspire.Dashboard 등

## Aspire 아키텍처
- AppHost: 앱 및 리소스 제어
- 서비스들: Web, Worker 등 앱
- 리소스: Redis, PostgreSQL 등
- Dashboard: 로그, 추적, 메트릭 제공
- Telemetry: OpenTelemetry 기반

## AppHost란?
- Aspire의 중앙 실행 진입점
- 서비스 및 리소스 구성/실행

```csharp
builder.AddProject("MyWebApp", "mywebapp");
builder.AddRedis("redis");
```

## ServiceDefaults()란?
- 서비스 공통 설정 정의
- OpenTelemetry, Metrics, Logging 적용

```csharp
app.ServiceDefaults();
```

## Aspire 개발 유형
### 유형 1: AppHost 메인
- 중앙 진입점
- Dashboard 자동 실행

### 유형 2: Dashboard 단독
- 기존 앱 유지
- OpenTelemetry 수동 설정

## 유형 1 vs 유형 2 비교

| 항목 | 유형 1: AppHost 기반 | 유형 2: Dashboard 단독 |
|------|----------------------|--------------------------|
| 시작점 | AppHost Program.cs | 기존 앱 유지 |
| 서비스 실행 방식 | AppHost에서 통합 실행 | 개별 실행 |
| Dashboard 연동 | 자동 포함 | 수동 연동 필요 |
| 외부 리소스 구성 | 코드 기반 자동 구성 | 별도 Docker 구성 필요 |
| 학습 곡선 | 낮음 | 높음 |

## Aspire Dashboard 화면 설명
- 구조화 로그: JSON 기반 로그 필터링
- 추적: 서비스 간 호출 시각화
- 매트릭: 지표 수집 및 시각화

## OpenTelemetry 구조
- Logs → Console & Dashboard
- Traces → Zipkin/Jaeger
- Metrics → Prometheus/OTLP
- ServiceDefaults 통해 자동 연결

## aspir8을 활용한 K8s 배포
- AppHost 기반 설정을 Kubernetes YAML로 변환

```bash
aspir8 --output-format kubernetes-yaml
--output-dir ./k8s
```
