# 🟢 Kafka 설치 확인 & 동작 테스트 가이드

## 1️⃣ Pod 상태 확인

```bash
kubectl get pods -n kafka
kubectl logs kafka-0 -n kafka
kubectl logs zookeeper-0 -n kafka
```

* 두 Pod 모두 **Running** 상태여야 정상
* 로그에 에러 없이 `started` 메시지 있으면 OK

---

## 2️⃣ 테스트용 Client Pod 실행

Kafka CLI 도구를 포함한 Pod 실행:

```bash
kubectl run kafka-client \
  --rm -it \
  --image=bitnami/kafka:3.8.0 \
  --namespace kafka \
  -- bash
```

> 이 Pod 내부에서 producer/consumer 테스트를 진행합니다.

---

## 3️⃣ 토픽 생성 및 확인

```bash
# 토픽 생성
kafka-topics.sh \
  --create \
  --topic test-topic \
  --bootstrap-server kafka:9092 \
  --partitions 1 \
  --replication-factor 1

# 토픽 목록 조회
kafka-topics.sh --list --bootstrap-server kafka:9092
```

---

## 4️⃣ 메시지 전송 (Producer)

```bash
kafka-console-producer.sh \
  --broker-list kafka:9092 \
  --topic test-topic
```

➡ 여기서 메시지를 입력 후 Enter
예: `hello kafka`

---

## 5️⃣ 메시지 수신 (Consumer)

다른 터미널에서 동일한 `kafka-client` Pod 실행 후:

```bash
kafka-console-consumer.sh \
  --bootstrap-server kafka:9092 \
  --topic test-topic \
  --from-beginning
```

➡ Producer에서 보낸 `hello kafka` 메시지가 보이면 정상 동작 ✅

---

## 6️⃣ 외부 접근 (선택 사항)

* NodePort Service 생성 → `PLAINTEXT://nodeIP:port` 로 접속
* LoadBalancer / Ingress Controller 활용 가능
* 외부 PC에서 테스트 시 `kafkacat`, `kafka-console-producer/consumer` 사용 가능
