# KW_REAL_DATA 순간 거래량 알림 시스템

이 문서는 `kw-real-data` Kafka 스트림을 이용하여, 순간 거래량 1000건 이상 시 C#에서 메일 발송하는 전체 시스템을 정리한 자료입니다.

---

## 1. ksqlDB 설정

### 1.1 Stream 생성
```sql
CREATE STREAM KW_REAL_DATA (
    stockCd STRING,
    hhmmss STRING,
    currPrc BIGINT,
    accStockVol BIGINT,
    regDt STRING,
    exCd STRING
) WITH (
    KAFKA_TOPIC='kw-real-data-topic',
    VALUE_FORMAT='JSON',
    KEY_FORMAT='KAFKA',
    PARTITIONS=1
);
```

### 1.2 HOPPING Window 기반 집계 테이블
```sql
CREATE TABLE KW_REAL_DATA_COUNT AS
SELECT stockCd,
       COUNT(*) AS cnt
FROM KW_REAL_DATA
WINDOW HOPPING (SIZE 1 MINUTE, ADVANCE BY 1 SECOND)
GROUP BY stockCd;
```

### 1.3 조건 기반 Alert Stream (토픽 발행)
```sql
CREATE STREAM KW_ALERT WITH (KAFKA_TOPIC='kw-alert-topic') AS
SELECT stockCd, cnt, ROWTIME AS alertTime
FROM KW_REAL_DATA_COUNT
WHERE cnt >= 1000
EMIT CHANGES;
```

- `KW_ALERT` 스트림은 Kafka 토픽 `kw-alert-topic`에 조건 충족 레코드를 발행합니다.
- EMIT CHANGES를 통해 실시간으로 업데이트됩니다.

---

## 2. C# 코드 예제

### 2.1 Kafka Producer (테스트용 자동 데이터 생성)
```csharp
using Confluent.Kafka;
using System.Text.Json;

var config = new ProducerConfig { BootstrapServers = "localhost:9092" };
using var producer = new ProducerBuilder<string, string>(config).Build();

for (int i = 0; i < 1000; i++)
{
    var data = new {
        stockCd = "005930",
        currPrc = 72700 + i % 10,
        hhmmss = DateTime.Now.ToString("HHmmss"),
        accStockVol = 30 + i,
        regDt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff"),
        exCd = "NXT"
    };

    string json = JsonSerializer.Serialize(data);
    await producer.ProduceAsync("kw-real-data-topic", new Message<string, string> { Key = data.stockCd, Value = json });
}
```

### 2.2 Kafka Consumer (Alert 토픽 구독 및 메일 발송)
```csharp
using Confluent.Kafka;
using System.Text.Json;

var config = new ConsumerConfig {
    BootstrapServers = "localhost:9092",
    GroupId = "alert-consumer",
    AutoOffsetReset = AutoOffsetReset.Earliest
};

using var consumer = new ConsumerBuilder<string, string>(config).Build();
consumer.Subscribe("kw-alert-topic");

while (true)
{
    var consumeResult = consumer.Consume();
    var record = JsonSerializer.Deserialize<JsonElement>(consumeResult.Message.Value);

    int count = record.GetProperty("cnt").GetInt32();
    string stockCd = record.GetProperty("stockCd").GetString();

    // 조건은 이미 ksqlDB에서 적용되었으므로 바로 알림
    sendMail(new { Subject = $"거래건수 초과: {stockCd}", Body = $"1분 거래 건수: {count}" });
}
```

> `sendMail(var params)` 함수는 기존 구현을 그대로 사용

---

## 3. 전체 시스템 구조 그림

```text
+-------------------+       +------------------+       +-----------------+
| C# Producer       |  ---> | Kafka Topic      |  ---> | ksqlDB Stream   |
| (kw-real-data)    |       | kw-real-data     |       | KW_REAL_DATA    |
+-------------------+       +------------------+       +-----------------+
                                      |
                                      v
                              +------------------+
                              | HOPPING Window   |
                              | KW_REAL_DATA_COUNT |
                              +------------------+
                                      |
                              cnt >= 1000 조건
                                      v
                              +------------------+
                              | KW_ALERT Stream  |
                              | (kw-alert-topic) |
                              +------------------+
                                      |
                                      v
                             +-----------------+
                             | C# Consumer     |
                             | sendMail()      |
                             +-----------------+
```

- HOPPING WINDOW로 1초 단위 슬라이딩 집계 → 순간 거래량 감지
- 조건 만족 시 KW_ALERT 토픽 발행 → C# Consumer에서 메일 발송

---

## ✅ 특징
- 실시간 거래 데이터 스트리밍 처리
- 순간거래량 1000건 감지 시 즉시 알림  
- 확장 가능: 다른 조건/종목도 ksqlDB 조건 추가로 처리 가능
- 테스트 환경에서 Docker-compose + Kafka + ksqlDB + C# 완전 연동 가능

