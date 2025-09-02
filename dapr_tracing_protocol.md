| Observability 도구                          | 지원 프로토콜/방식                    | 비고                                                |
| ----------------------------------------- | ----------------------------- | ------------------------------------------------- |
| **Google Cloud Operations (Cloud Trace)** | **OTEL (OTLP)**               | OpenTelemetry Collector를 통해 GCP Trace exporter 사용 |
| **AWS X-Ray**                             | **OTEL (OTLP)**               | OTEL Collector에서 AWS X-Ray exporter 필요            |
| **New Relic**                             | **Zipkin** or **OTEL (OTLP)** | Zipkin API 엔드포인트 전송 또는 OTEL Collector 경유          |
| **Azure Monitor (App Insights)**          | **OTEL (OTLP)**               | OTEL Collector에서 Azure Monitor exporter 필요        |
| **Datadog**                               | **OTEL (OTLP)**               | Collector에서 Datadog exporter 사용                   |
| **Zipkin**                                | **Zipkin**                    | Dapr에서 직접 Zipkin endpoint로 전송 가능                  |
| **Jaeger**                                | **Zipkin** or **OTEL (OTLP)** | Jaeger는 Zipkin 형식 또는 OTEL 포맷 모두 수용 가능             |
| **SignalFx (Splunk Observability)**       | **OTEL (OTLP)**               | Collector에서 SignalFx/Splunk exporter 사용           |
| **Dash0**                                 | **OTEL (OTLP)** or **Zipkin** | 공식적으로 두 가지 방식 모두 지원                               |
