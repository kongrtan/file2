
# Kafka, Dapr Pub/Sub 관련 Q&A

## 1. Kafka client 직접 사용 vs Dapr pub/sub

| 항목 | Kafka client 직접 사용 | Dapr Pub/Sub (Kafka binding) |
|------|--------------------|----------------------------|
| **설정/운영 난이도** | 직접 Kafka 클러스터 연결, topic 관리, offset 관리 필요 | Dapr가 Kafka binding을 추상화, 구성만으로 pub/sub 사용 가능 |
| **과거 메시지 접근** | `auto.offset.reset`, `seek()` 등 Kafka API 활용하여 특정 오프셋부터 과거 메시지 재처리 가능 | 일반적으로 실시간 메시지 수신 중심. 과거 메시지 조회는 직접 Kafka offset 관리 필요 |
| **확장성** | 애플리케이션이 직접 Kafka 파티션/컨슈머 그룹 관리, 고급 튜닝 가능 | Dapr가 컨슈머 그룹/배포 관리 추상화, 코드 단순화 가능 |
| **내결함성 / 재시도** | Kafka consumer 자체 재시도, offset commit 제어 가능 | Dapr는 at-least-once delivery 보장. 자세한 offset 관리는 Dapr에 위임 |
| **멀티 메시징 시스템 지원** | Kafka 전용 | Dapr를 사용하면 Kafka 외 다른 메시징 시스템으로 교체 가능 |
| **코드 복잡도** | 상대적으로 높음 | 낮음 (Dapr SDK 호출만으로 pub/sub 처리 가능) |

### 결론
- Kafka가 고정이고 과거 메시지 조회와 장애 시 재처리가 중요하면 **Kafka client 사용** 유리  
- 단순 pub/sub, 다양한 메시징 시스템 추상화가 필요하면 **Dapr 사용** 편리  

## 2. 클라이언트 장애 시 메시지 수신 가능 여부

### Kafka client
- Offset 기반 소비 → 클라이언트 장애 후 재접속 시 마지막 커밋된 offset부터 메시지 수신 가능  
- 조건:
  - `enable.auto.commit=false` → 수동 커밋 가능
  - Kafka retention 기간 내 메시지 존재

### Dapr pub/sub
- 기본적으로 실시간 메시지 처리 중심 → 장애 동안 발생한 메시지 놓칠 가능성  
- 과거 메시지 수신 위해서는 별도 offset 관리 필요  

## 3. Dapr pub/sub `initialOffset`

- `"earliest"` → 컨슈머 그룹에 offset 없으면 토픽 맨 처음 메시지부터 수신  
- `"latest"` → 컨슈머 그룹에 offset 없으면 그 시점 이후 메시지부터 수신  
- 이미 컨슈머 그룹에 offset 존재 시 초기 설정 무시  

**결론:**  
`initialOffset=newest(latest)` → 최초 접속 시 이후 메시지만 수신  
과거 메시지 모두 받고 싶으면 → `initialOffset=earliest`  

## 4. 컨슈머 그룹과 토픽 관계

- 토픽(Topic)
  - 메시지 저장 단위, 파티션으로 나눔
  - 독립적 존재
- 컨슈머 그룹(Consumer Group)
  - 메시지 처리 단위, offset 관리
  - 토픽에 종속되지 않음
- 하나의 토픽에 여러 컨슈머 그룹 가능
- 하나의 컨슈머 그룹은 여러 토픽 구독 가능

### 메시지 처리 예시

```
토픽: Orders (3 파티션: P0, P1, P2)

컨슈머 그룹 A (2 컨슈머)
  C1 -> P0, P2
  C2 -> P1

컨슈머 그룹 B (1 컨슈머)
  C3 -> P0, P1, P2
``

- 그룹 A: 각 컨슈머가 파티션 나눠서 처리 → 메시지 중복 없음  
- 그룹 B: 모든 메시지를 단일 컨슈머가 처리  
- 그룹 A와 B는 독립적 → 서로 메시지 영향 없음

## 5. 핵심 정리

1. 토픽과 컨슈머 그룹은 종속적이지 않음  
2. 토픽 하나에 여러 컨슈머 그룹 가능  
3. 컨슈머 그룹은 offset 단위로 메시지 처리 상태 관리  
4. Dapr pub/sub 사용 시 `initialOffset` 설정이 중요, 과거 메시지 필요하면 `earliest`  
5. Kafka client 직접 사용 시 클라이언트 장애에도 offset 기반 재처리 가능
