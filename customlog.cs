public class TruncateFileSink : ILogEventSink, IDisposable
{
    private readonly string _path;
    private readonly long _maxSizeBytes;
    private readonly object _lock = new object();
    private StreamWriter _writer;

    public TruncateFileSink(string path, long maxSizeBytes)
    {
        _path = path;
        _maxSizeBytes = maxSizeBytes;
        _writer = new StreamWriter(new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.Read))
        {
            AutoFlush = true
        };
    }

    public void Emit(LogEvent logEvent)
    {
        lock (_lock)
        {
            var formatted = logEvent.RenderMessage() + Environment.NewLine;

            // 파일 크기 확인
            var fileInfo = new FileInfo(_path);
            if (fileInfo.Exists && fileInfo.Length + formatted.Length > _maxSizeBytes)
            {
                // 파일 삭제 후 새로 오픈
                _writer.Dispose();
                File.Delete(_path);
                _writer = new StreamWriter(new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    AutoFlush = true
                };
            }

            _writer.Write(formatted);
        }
    }

    public void Dispose() => _writer?.Dispose();
}



/////
{
  "Serilog": {
    "Using": [ "MyApp.Logging" ], // TruncateFile 확장 메서드 들어있는 어셈블리 네임스페이스
    "WriteTo": [
      {
        "Name": "TruncateFile",
        "Args": {
          "path": "log.txt",
          "maxSizeBytes": 100000
        }
      }
    ]
  }
}








