using Microsoft.AspNetCore.Mvc;
using RestSharp;
using System.Text.Json;

namespace OlamaQdrantSample.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecommendController : ControllerBase
    {
        private readonly RestClient _ollamaClient = new("http://localhost:11434");
        private readonly RestClient _qdrantClient = new("http://localhost:6333");

        [HttpPost("recommend")]
        public async Task<IActionResult> Recommend([FromBody] RecommendRequest request)
        {
            // 1. Olama에서 질문 임베딩 생성
            var embeddingReq = new RestRequest("api/embeddings", Method.Post);
            embeddingReq.AddJsonBody(new { model = "bge-small", prompt = request.Question });
            var embeddingRes = await _ollamaClient.ExecuteAsync(embeddingReq);
            if (!embeddingRes.IsSuccessful) return StatusCode(500, "Failed to get embedding");

            var vector = JsonDocument.Parse(embeddingRes.Content)
                .RootElement.GetProperty("embedding")
                .EnumerateArray().Select(x => x.GetSingle()).ToArray();

            // 2. Qdrant에서 유사한 상품 검색
            var searchReq = new RestRequest("/collections/products/points/search", Method.Post);
            searchReq.AddJsonBody(new
            {
                vector = vector,
                top = 3,
                with_payload = true
            });
            var searchRes = await _qdrantClient.ExecuteAsync(searchReq);
            if (!searchRes.IsSuccessful) return StatusCode(500, "Qdrant search failed");

            var searchJson = JsonDocument.Parse(searchRes.Content);
            var contexts = string.Join("\n", searchJson.RootElement.GetProperty("result")
                .EnumerateArray()
                .Select(p => "- " + p.GetProperty("payload").GetProperty("name").GetString()));

            // 3. Olama로 RAG 응답 생성
            var ragReq = new RestRequest("api/chat", Method.Post);
            ragReq.AddJsonBody(new
            {
                model = "llama3",
                messages = new[] {
                    new { role = "system", content = $"다음 상품 설명을 바탕으로 추천해줘:\n{contexts}" },
                    new { role = "user", content = request.Question }
                },
                stream = false
            });
            var ragRes = await _ollamaClient.ExecuteAsync(ragReq);
            if (!ragRes.IsSuccessful) return StatusCode(500, "Failed to get RAG answer");

            var ragAnswer = JsonDocument.Parse(ragRes.Content).RootElement.GetProperty("message").GetProperty("content").GetString();
            return Ok(new { answer = ragAnswer });
        }
    }

    public class RecommendRequest
    {
        public string Question { get; set; } = string.Empty;
    }
}
