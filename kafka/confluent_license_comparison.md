## Confluent Community vs Enterprise 기능 비교 (Markdown)

| 기능 범주 | Community Edition (무료) | Enterprise Edition (유료, 라이선스 키 필요) | Docker Hub 주소 |
|-----------|--------------------------|--------------------------------------------|----------------|
| Kafka Broker | Apache Kafka 기본 기능 | Confluent 향상 기능 포함, 멀티 브로커 고급 설정 | [cp-kafka](https://hub.docker.com/r/confluentinc/cp-kafka) |
| Control Center | 제한적 기능, 단일 브로커 환경 모니터링 | 전체 기능 제공, 모니터링, 대시보드, 알림 | [cp-control-center](https://hub.docker.com/r/confluentinc/cp-control-center) |
| Schema Registry | 기본 스키마 관리 | 보안 플러그인, 권한 제어, 고급 관리 기능 | [cp-schema-registry](https://hub.docker.com/r/confluentinc/cp-schema-registry) |
| ksqlDB | 기본 스트림 처리 기능 | 고급 기능, 확장형 쿼리, Enterprise 연동 기능 | [cp-ksqldb-server](https://hub.docker.com/r/confluentinc/cp-ksqldb-server) |
| Connectors | 일부 커넥터 사용 가능 | 모든 상용 커넥터(100개 이상) 사용 가능 | [cp-kafka-connect](https://hub.docker.com/r/confluentinc/cp-kafka-connect) |
| RBAC (권한 관리) | 제공 안 함 | 지원, 세분화된 사용자 권한 관리 가능 | [cp-control-center](https://hub.docker.com/r/confluentinc/cp-control-center) |
| Audit Logs (감사 로그) | 제공 안 함 | 지원, 보안 감사 및 컴플라이언스 대응 가능 | [cp-control-center](https://hub.docker.com/r/confluentinc/cp-control-center) |
| Cluster Linking | 제공 안 함 | 지원, 멀티 클러스터 연결 가능 | [cp-kafka](https://hub.docker.com/r/confluentinc/cp-kafka) |
| Tiered Storage (장기 저장소) | 제공 안 함 | 지원, 비용 효율적인 스토리지 관리 가능 | [cp-kafka](https://hub.docker.com/r/confluentinc/cp-kafka) |
| 라이선스 필요 여부 | 필요 없음 | 필요 (라이선스 키 등록 필수) | - |
| 사용 환경 | 개발/테스트, 단일 브로커 | 프로덕션, 멀티 브로커, 고가용성 환경 | - |

### 🔹 핵심 요약

1. **Community Edition**
   - 개발, 테스트, 소규모 환경에서 무료 사용 가능
   - 단일 브로커 및 기본 기능만 제공
   - 라이선스 키 필요 없음 → 설치 및 사용 안전

2. **Enterprise Edition**
   - 프로덕션, 멀티 브로커, 보안/권한/모니터링 필요 환경
   - Enterprise 기능 활성화를 위해 **라이선스 키 등록 필수**
   - 무료 사용 범위를 넘어서는 기능을 사용하면 계약 위반 가능