| 구분 | **pubsub.kafka** | **binding.kafka** |
|------|------------------|-------------------|
| **소속 Building Block** | Pub/Sub | Bindings |
| **주요 목적** | 메시지 브로커 추상화 (이벤트 기반 통신) | 단순 I/O (Kafka에 읽기/쓰기) |
| **메시지 형식** | 기본적으로 CloudEvent(JSON), `rawPayload: true` 시 원본 그대로 | 단순 바이트/JSON (CloudEvent 래핑 없음) |
| **사용 방식** | - 발행: `POST /v1.0/publish/<topic>` <br> - 구독: 앱 엔드포인트에 POST 전달 | - 출력: `POST /v1.0/bindings/<name>` <br> - 입력: Kafka 메시지를 앱 엔드포인트에 POST |
| **기능 지원** | Retry, DLQ, at-least-once 보장, 브로커 교체 가능 | 단순 produce/consume, 고급 기능 없음 |
| **관측성 (Tracing/Logging)** | CloudEvent 기반 tracing 가능 | 기본 tracing만, CloudEvent 연계 없음 |
| **브로커 교체 용이성** | 높음 (RabbitMQ, Redis Streams, Azure Service Bus 등으로 교체 가능) | 낮음 (Kafka 전용) |
| **적합한 시나리오** | 이벤트 드리븐 아키텍처, 안정성/재처리/확장 필요 | 단순 Kafka 입출력, 고급 기능 불필요한 경우 |
