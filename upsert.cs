using Microsoft.AspNetCore.Mvc;
using RestSharp;
using System.Text.Json;

namespace OlamaQdrantSample.Controllers
{
    [ApiController]
    [Route("api/v1/qdrant")]
    public class QdrantController : ControllerBase
    {
        private readonly RestClient _qdrantClient = new("http://localhost:6333");

        [HttpPost("upsert")]
        public async Task<IActionResult> UpsertVector([FromBody] UpsertRequest request)
        {
            // 1. 컬렉션 존재 여부 확인
            var checkReq = new RestRequest("/collections", Method.Get);
            var checkRes = await _qdrantClient.ExecuteAsync(checkReq);
            if (!checkRes.IsSuccessful) return StatusCode(500, "Failed to fetch collections");

            var collections = JsonDocument.Parse(checkRes.Content).RootElement
                .GetProperty("collections")
                .EnumerateArray()
                .Select(c => c.GetProperty("name").GetString());

            if (!collections.Contains(request.Collection))
            {
                // 2. 컬렉션 생성
                var createReq = new RestRequest($"/collections/{request.Collection}", Method.Put);
                createReq.AddJsonBody(new
                {
                    vectors = new { size = request.Vector.Length, distance = "Cosine" }
                });
                var createRes = await _qdrantClient.ExecuteAsync(createReq);
                if (!createRes.IsSuccessful) return StatusCode(500, "Failed to create collection");
            }

            // 3. 벡터 업서트
            var upsertReq = new RestRequest($"/collections/{request.Collection}/points?wait=true", Method.Put);
            upsertReq.AddJsonBody(new
            {
                points = new[]
                {
                    new
                    {
                        id = request.Id,
                        vector = request.Vector,
                        payload = request.Payload
                    }
                }
            });

            var upsertRes = await _qdrantClient.ExecuteAsync(upsertReq);
            if (!upsertRes.IsSuccessful)
                return StatusCode(500, "Vector upsert failed: " + upsertRes.Content);

            return Ok("Vector upserted successfully");
        }
    }

    public class UpsertRequest
    {
        public string Collection { get; set; } = string.Empty;
        public int Id { get; set; }
        public float[] Vector { get; set; } = [];
        public Dictionary<string, object> Payload { get; set; } = new();
    }
}
