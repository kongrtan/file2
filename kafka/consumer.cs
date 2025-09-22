	public partial class MainForm : Form {

		private readonly IConsumer<string, string> _consumer;

		public MainForm() {
			//var config = new ProducerConfig {
			//	BootstrapServers = "192.168.56.103:9092",
			//	LingerMs = 5,           // 실시간성 유지
			//	BatchSize = 32 * 1024   // 32KB
			//};


			var config = new ConsumerConfig {
				BootstrapServers = "192.168.56.103:9092",
				GroupId = "alert-consumer",
				AutoOffsetReset = AutoOffsetReset.Latest
			};

			_consumer = new ConsumerBuilder<string, string>(config).Build();
			_consumer.Subscribe("KW_ALERT");

			InitializeComponent();

		}

		private void btnStart_Click(object sender, EventArgs e) {
			Task.Run(() => Subscribe());
			Console.WriteLine("구독 시작");
		}


		private void Subscribe() {
			while (true) {
				var cr = _consumer.Consume();
				var record = JsonSerializer.Deserialize<JsonElement>(cr.Message.Value);

				string stockCd = cr.Key;
				int count = record.GetProperty("CNT").GetInt32();

				Console.WriteLine("거래량 증가 " + stockCd + ": " + count);
			}

		}

	}
