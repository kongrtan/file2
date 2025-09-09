using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Confluent.Kafka;

class Simulator
{
    public static async Task Run()
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = "localhost:9092"
        };

        using var producer = new ProducerBuilder<Null, string>(producerConfig).Build();

        using var fs = new FileStream("bigfile.txt", FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024);
        using var sr = new StreamReader(fs);

        string? line = await sr.ReadLineAsync();
        if (line == null) return;

        // 첫 row에서 기준 시각 추출
        var firstTime = DateTime.ParseExact(
            line.Split(',')[0], // "2025-09-08 08:01:11.234"
            "yyyy-MM-dd HH:mm:ss.fff",
            CultureInfo.InvariantCulture);

        var startOffset = DateTime.Now - firstTime;

        // 첫 줄 전송
        await producer.ProduceAsync("sim-topic", new Message<Null, string> { Value = line });

        while ((line = await sr.ReadLineAsync()) != null)
        {
            var ts = DateTime.ParseExact(
                line.Split(',')[0],
                "yyyy-MM-dd HH:mm:ss.fff",
                CultureInfo.InvariantCulture);

            var targetTime = ts + startOffset;
            var delay = targetTime - DateTime.Now;

            if (delay > TimeSpan.Zero)
                await Task.Delay(delay);

            await producer.ProduceAsync("sim-topic", new Message<Null, string> { Value = line });
        }

        producer.Flush();
    }
}
