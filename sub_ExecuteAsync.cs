protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    var cfg = BuildConsumerConfig(_settings);
    using var consumer = new ConsumerBuilder<string, string>(cfg)
        .SetErrorHandler((_, e) => Console.WriteLine($"[consumer error] {e.Reason}"))
        .SetLogHandler((_, log) => Console.WriteLine($"[librdkafka] {log.Level}: {log.Message}"))
        .SetPartitionsAssignedHandler((c, parts) =>
        {
            Console.WriteLine($"[assigned] {string.Join(", ", parts)}");
            c.Assign(parts); // 수동 할당 처리
        })
        .SetPartitionsRevokedHandler((c, parts) =>
        {
            Console.WriteLine($"[revoked] {string.Join(", ", parts)}");
            c.Unassign();
        })
        .Build();

    consumer.Subscribe(_settings.Topic);
    Console.WriteLine("Subscribed, entering consume loop...");

    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            // 타임아웃 있는 Consume 사용(디버깅/모니터링에 유리)
            var cr = consumer.Consume(TimeSpan.FromMilliseconds(500));
            if (cr == null)
            {
                // 타임아웃: 메시지 없음
                var assigned = consumer.Assignment;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] no message - assigned: {string.Join(", ", assigned.Select(a => a.ToString()))}");
                continue;
            }

            Console.WriteLine($"[consumed] {cr.TopicPartitionOffset} key={cr.Message.Key} valueLen={cr.Message.Value?.Length ?? 0}");
            // TODO: WebSocket 브로드캐스트 로직
            consumer.Commit(cr); // 필요시
        }
        catch (ConsumeException ex)
        {
            Console.WriteLine($"[consume exception] {ex.Error.Reason}");
        }
        catch (OperationCanceledException)
        {
            // graceful shutdown
            break;
        }
    }

    consumer.Close();
}
