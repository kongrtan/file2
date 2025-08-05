# 🧠 Olama + Qdrant + ASP.NET Core 샘플 프로젝트

이 프로젝트는 Olama에서 임베딩을 생성하고, Qdrant에 저장 및 검색하며, ASP.NET Core에서 이를 호출하여 추천 시스템 또는 RAG를 구성하는 기본 예제입니다.

---

## 📦 구성

- **Olama** (http://localhost:11434)
- **Qdrant** (http://localhost:6333)
- **ASP.NET Core Web API** (.NET 8 이상)

---

## 🔧 예제: Olama 임베딩 생성 → Qdrant 업서트

```csharp
using RestSharp;
using System.Text.Json;

// 1. Olama로 임베딩 생성
var embeddingClient = new RestClient("http://localhost:11434");
var embeddingRequest = new RestRequest("api/embeddings", Method.Post);
embeddingRequest.AddJsonBody(new {
    model = "bge-small",
    prompt = "가볍고 통기성이 좋은 러닝화"
});

var embeddingResponse = await embeddingClient.ExecuteAsync(embeddingRequest);
var vector = JsonDocument.Parse(embeddingResponse.Content)
    .RootElement.GetProperty("embedding")
    .EnumerateArray()
    .Select(x => x.GetSingle())
    .ToArray();

// 2. Qdrant에 업서트
var qdrantClient = new RestClient("http://localhost:6333");
var upsertRequest = new RestRequest("/collections/products/points?wait=true", Method.Put);
upsertRequest.AddJsonBody(new {
    points = new[] {
        new {
            id = 1,
            vector = vector,
            payload = new {
                name = "나이키 러닝화",
                category = "shoes",
                brand = "Nike"
            }
        }
    }
});

var upsertResponse = await qdrantClient.ExecuteAsync(upsertRequest);
Console.WriteLine("업서트 성공 여부: " + upsertResponse.IsSuccessful);
```

---

## 🔍 예제: Qdrant 유사 검색 → Olama로 RAG 응답 생성

```csharp
// 질문 입력
string question = "가볍고 쿠셔닝이 좋은 러닝화를 추천해줘";

// 1. Olama 임베딩 → Qdrant 검색 (top 3)
var searchRequest = new RestRequest("/collections/products/points/search", Method.Post);
searchRequest.AddJsonBody(new {
    vector = vector, // 위에서 생성한 질문 임베딩
    top = 3,
    with_payload = true
});
var searchResponse = await qdrantClient.ExecuteAsync(searchRequest);
var contexts = ExtractProductDescriptions(searchResponse.Content); // 추출 함수 정의 필요

// 2. RAG 질문 구성
var ragRequest = new RestRequest("api/chat", Method.Post);
ragRequest.AddJsonBody(new {
    model = "llama3",
    messages = new[] {
        new { role = "system", content = $"다음 상품 설명을 기반으로 추천해줘:
{contexts}" },
        new { role = "user", content = question }
    },
    stream = false
});
var ragResponse = await embeddingClient.ExecuteAsync(ragRequest);
Console.WriteLine("RAG 응답: " + ragResponse.Content);
```

---

## 📁 기타

- 모델이 설치되어 있지 않으면 Olama에 아래 API로 pull:
```bash
curl http://localhost:11434/api/pull -X POST -H "Content-Type: application/json" -d '{ "name": "llama3" }'
```

---

## ✅ 향후 확장 아이디어

- 카테고리 필터 추가 (`filter` → Qdrant 조건 검색)
- 사용자의 이전 선호도 기반 추천
- Blazor 프론트엔드 연결
- Docker로 Olama + Qdrant 구성 자동화

---

## 📌 참고 주소

- [Qdrant REST API Docs](https://qdrant.tech/documentation/rest/)
- [Ollama API Docs](https://github.com/jmorganca/ollama/blob/main/docs/api.md)