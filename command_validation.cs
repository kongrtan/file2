using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

[ApiController]
[Route("api/[controller]")]
public class CommandController : ControllerBase
{
    private readonly Dictionary<string, Func<JObject, Task<IActionResult>>> _handlers;

    public CommandController()
    {
        _handlers = new Dictionary<string, Func<JObject, Task<IActionResult>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["aa"] = HandleAA,
            ["bb"] = HandleBB,
            ["cc"] = HandleCC,
            ["dd"] = HandleDD
        };
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CommandRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Command))
            return BadRequest(new { error = "Missing command field" });

        if (!_handlers.TryGetValue(request.Command, out var handler))
        {
            return BadRequest(new { error = $"Unknown command: {request.Command}" });
        }

        try
        {
            return await handler(request.Data);
        }
        catch (Exception ex)
        {
            // fail-safe 처리 (예: 로깅, Splunk trace 등)
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private Task<IActionResult> HandleAA(JObject data)
    {
        // command=aa 처리 로직
        var value = data?["value"]?.ToObject<int>();
        return Task.FromResult<IActionResult>(Ok(new { result = $"Handled AA with value={value}" }));
    }

    private Task<IActionResult> HandleBB(JObject data)
    {
        return Task.FromResult<IActionResult>(Ok(new { result = "Handled BB" }));
    }

    private Task<IActionResult> HandleCC(JObject data)
    {
        return Task.FromResult<IActionResult>(Ok(new { result = "Handled CC" }));
    }

    private Task<IActionResult> HandleDD(JObject data)
    {
        return Task.FromResult<IActionResult>(Ok(new { result = "Handled DD" }));
    }
}
