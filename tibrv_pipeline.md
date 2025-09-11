# TIBRV 수신 데이터 DB 저장 파이프라인 정리

## 1️⃣ 수신 데이터 형식 예시
```
2025-01-01 10:10:23 (2025-01-01 10:10:23.000333Z): 
subject=_AAA.BBB.CCC1333, 
reply=_INBOX.345354.5645454454.3, 
message={DATA=350}
```

- **timestamp**: `2025-01-01 10:10:23.000333Z`
- **subject**: `_AAA.BBB.CCC1333`
- **reply**: `_INBOX.345354.5645454454.3`
- **message**: `{DATA=350}`

---

## 2️⃣ DB 스키마 (PostgreSQL)

```sql
-- subjects 테이블 (subject 계층 저장)
CREATE TABLE subjects (
    id BIGSERIAL PRIMARY KEY,
    level1 VARCHAR(100) NOT NULL,
    level2 VARCHAR(100),
    level3 VARCHAR(100),
    level4 VARCHAR(100),
    full_subject VARCHAR(500) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(level1, level2, level3, level4)
);

-- messages 테이블 (payload 저장)
CREATE TABLE messages (
    id BIGSERIAL PRIMARY KEY,
    subject_id BIGINT NOT NULL REFERENCES subjects(id) ON DELETE CASCADE,
    reply VARCHAR(500),
    payload JSONB NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 검색 최적화를 위한 인덱스
CREATE INDEX idx_subjects_levels ON subjects(level1, level2, level3, level4);
CREATE INDEX idx_messages_subject_id ON messages(subject_id);
```

---

## 3️⃣ DTO 클래스 (C#)

```csharp
public class TibrvMessageDto
{
    public DateTime Timestamp { get; set; }
    public string Subject { get; set; }
    public string Reply { get; set; }
    public string[] SubjectLevels { get; set; }
    public Dictionary<string, string> MessageData { get; set; }
}
```

---

## 4️⃣ 파서 (C#)

```csharp
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class TibrvMessageParser
{
    public static TibrvMessageDto Parse(string rawMessage)
    {
        // 1. Timestamp 추출
        var timestampMatch = Regex.Match(rawMessage, @"\(([\d\-:\.TZ]+)\)");
        DateTime timestamp = timestampMatch.Success
            ? DateTime.Parse(timestampMatch.Groups[1].Value)
            : DateTime.UtcNow;

        // 2. Subject 추출
        var subjectMatch = Regex.Match(rawMessage, @"subject=([\w\._]+),");
        string subject = subjectMatch.Success ? subjectMatch.Groups[1].Value : null;

        // 3. Reply 추출
        var replyMatch = Regex.Match(rawMessage, @"reply=([\w\._]+),");
        string reply = replyMatch.Success ? replyMatch.Groups[1].Value : null;

        // 4. Message 데이터 추출
        var messageMatch = Regex.Match(rawMessage, @"message=\{(.+?)\}");
        Dictionary<string, string> messageData = new();
        if (messageMatch.Success)
        {
            var pairs = messageMatch.Groups[1].Value.Split(',');
            foreach (var pair in pairs)
            {
                var kv = pair.Split('=');
                if (kv.Length == 2)
                {
                    messageData[kv[0].Trim()] = kv[1].Trim();
                }
            }
        }

        // 5. Subject Levels 분리
        string[] levels = subject?.Split('.') ?? Array.Empty<string>();

        return new TibrvMessageDto
        {
            Timestamp = timestamp,
            Subject = subject,
            Reply = reply,
            SubjectLevels = levels,
            MessageData = messageData
        };
    }
}
```

---

## 5️⃣ Dapper Repository (C#)

```csharp
using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Npgsql;
using System.Text.Json;

public class TibrvRepository
{
    private readonly string _connectionString;

    public TibrvRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InsertAsync(TibrvMessageDto dto)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        // 1. subjects 존재 여부 확인
        var subjectId = await connection.ExecuteScalarAsync<long?>(@"
            SELECT id FROM subjects 
            WHERE level1=@level1 AND level2=@level2 AND level3=@level3 AND level4=@level4
        ", new
        {
            level1 = dto.SubjectLevels.Length > 0 ? dto.SubjectLevels[0] : null,
            level2 = dto.SubjectLevels.Length > 1 ? dto.SubjectLevels[1] : null,
            level3 = dto.SubjectLevels.Length > 2 ? dto.SubjectLevels[2] : null,
            level4 = dto.SubjectLevels.Length > 3 ? dto.SubjectLevels[3] : null
        });

        // 2. 없으면 insert 후 id 반환
        if (subjectId == null)
        {
            subjectId = await connection.ExecuteScalarAsync<long>(@"
                INSERT INTO subjects (level1, level2, level3, level4, full_subject, created_at)
                VALUES (@level1, @level2, @level3, @level4, @full_subject, @created_at)
                RETURNING id
            ", new
            {
                level1 = dto.SubjectLevels.Length > 0 ? dto.SubjectLevels[0] : null,
                level2 = dto.SubjectLevels.Length > 1 ? dto.SubjectLevels[1] : null,
                level3 = dto.SubjectLevels.Length > 2 ? dto.SubjectLevels[2] : null,
                level4 = dto.SubjectLevels.Length > 3 ? dto.SubjectLevels[3] : null,
                full_subject = dto.Subject,
                created_at = dto.Timestamp
            });
        }

        // 3. messages insert
        await connection.ExecuteAsync(@"
            INSERT INTO messages (subject_id, reply, payload, created_at)
            VALUES (@subject_id, @reply, @payload::jsonb, @created_at)
        ", new
        {
            subject_id = subjectId,
            reply = dto.Reply,
            payload = JsonSerializer.Serialize(dto.MessageData),
            created_at = dto.Timestamp
        });
    }
}
```

---

## 6️⃣ 사용 예제

```csharp
string connString = "Host=localhost;Port=5432;Username=postgres;Password=1234;Database=testdb";
var repo = new TibrvRepository(connString);

string rawMessage = "2025-01-01 10:10:23 (2025-01-01 10:10:23.000333Z): subject=_AAA.BBB.CCC1333, reply=_INBOX.345354.5645454454.3, message={DATA=350}";

TibrvMessageDto dto = TibrvMessageParser.Parse(rawMessage);

await repo.InsertAsync(dto);

Console.WriteLine("Insert 완료!");
```

---

## ✅ 요약
- `tibrvlisten` 로그 → **파싱** → DTO 변환  
- `subjects` 테이블: subject 계층 구조 저장  
- `messages` 테이블: payload + reply 저장  
- `Dapper` 활용하여 DB에 insert  
