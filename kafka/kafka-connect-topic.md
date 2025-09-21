```
docker exec -it kafka bash
```

```
# 잘못된 토픽 삭제
kafka-topics --bootstrap-server kafka:9093 \
  --delete --topic _connect-configs

kafka-topics --bootstrap-server kafka:9093 \
  --delete --topic _connect-offsets

kafka-topics --bootstrap-server kafka:9093 \
  --delete --topic _connect-status
```

```
# 새로 생성 (cleanup.policy=compact)
kafka-topics --bootstrap-server kafka:9093 \
  --create --topic _connect-configs \
  --partitions 1 --replication-factor 1 \
  --config cleanup.policy=compact

kafka-topics --bootstrap-server kafka:9093 \
  --create --topic _connect-offsets \
  --partitions 50 --replication-factor 1 \
  --config cleanup.policy=compact

kafka-topics --bootstrap-server kafka:9093 \
  --create --topic _connect-status \
  --partitions 5 --replication-factor 1 \
  --config cleanup.policy=compact

```

```
docker exec -it ksqldb-cli ksql http://ksqldb:8088

```

```
CREATE STREAM KW_REAL_DATA (
    stockCd STRING,
    hhmmss STRING,
    currPrc BIGINT,
    accStockVol BIGINT,
    regDt STRING,
    exCd STRING
) WITH (
    KAFKA_TOPIC = 'kw-real-data-topic',
    VALUE_FORMAT = 'JSON',
    KEY_FORMAT = 'KAFKA',
    PARTITIONS = 1
);

```

### 스트림 생성 후 메시지
```
 Message
----------------
 Stream created
----------------
```
