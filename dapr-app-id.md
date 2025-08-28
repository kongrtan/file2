# Dapr + Kafka Q&A

## Q1: Dapr에서 Kafka 접속 시 app-id 영향

**질문:** dapr 에 kafka로 접속시 동일한 app-id를 사용하면 마지막에 접속한 것만 connect가 살아 있는데 접속할때 어떤 정보를 사용하나?

**답변:**

- Dapr의 pub/sub Kafka 컴포넌트에서 `app-id`는 Dapr 인스턴스 식별 및 **consumer group 이름**으로 사용됩니다.
- Kafka는 동일 consumer group 내에서 파티션을 한 번만 소비하도록 설계되어 있어, 동일 `app-id` 사용 시 마지막 연결된 인스턴스만 메시지를 받는 것처럼 보임.

**Kafka 접속 시 사용되는 정보:**
| 항목 | 설명 |
|------|------|
| `bootstrapServers` | Kafka 브로커 주소 |
| `auth` | SASL/SSL 인증 정보 |
| `groupID` | Dapr 내부에서 app-id 기반으로 자동 생성 |
| `clientID` | Kafka 클라이언트 식별자 (옵션) |
| `topics` | 구독할 Kafka 토픽 목록 |

**해결 방법:**
1. 서로 다른 app-id 사용
2. 동일 app-id, 다중 인스턴스 → 토픽 파티션 수 조정 필요
3. `consumerGroup` 명시

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: mykafka
  namespace: default
spec:
  type: pubsub.kafka
  version: v1
  metadata:
  - name: brokers
    value: "kafka:9092"
  - name: consumerGroup
    value: "custom-group-id"
```

---

## Q2: Deployment spec replicas:2일 때 Kafka 연결

**질문:** app-id = abc를 넣고 deployment spec에 replicas:2로 설정하면 두번째 시작한 파드에서만 kafka에 연결되는 건가?

**답변:**

- Dapr는 동일 app-id → 동일 consumer group
- Kafka consumer group은 파티션 단위로 메시지를 분배
- 토픽 파티션 1개 + replicas:2 → 마지막 Pod만 메시지를 받음
- 해결 방법:
  1. 토픽 파티션 수 ≥ replica 수
  2. Dapr Kafka 컴포넌트에서 `consumerGroup`을 Pod별로 유니크하게 설정

```yaml
metadata:
  name: mykafka
spec:
  type: pubsub.kafka
  metadata:
  - name: brokers
    value: "kafka:9092"
  - name: consumerGroup
    value: "abc-{{POD_NAME}}"  # Pod마다 고유 group
```

---

## Q3: 재접속 처리와 StatefulSet

**질문:** 재접속 처리하려면 deployment가 아닌 stsfulset으로 해야하나?

**답변:**

- Deployment vs StatefulSet:
| 항목 | Deployment | StatefulSet |
|------|-----------|------------|
| Pod 이름 | 무작위 | 순서대로 고유(pod-0, pod-1) |
| 네트워크 ID | 랜덤 | 고정 DNS 이름 |
| 볼륨 | 공유 불가 | Pod별 고정 PVC 가능 |
| 재시작 시 ID 유지 | 불가 | 가능 |

- Kafka 재접속과 app-id 문제:
  - Deployment: Pod 재시작 시 이름 바뀌고 rebalancing 발생
  - StatefulSet: Pod 이름 고정 → consumerGroup을 Pod 이름 기반으로 유니크하게 설정 가능

**결론:**
- 고정 ID 기반 재접속이 필요하면 StatefulSet 권장
- Deployment 사용 시 Pod 이름 기반으로 consumerGroup 유니크하게 만들면 안정적

