using bumevent.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace BmuEvent_API.Controllers
{
    [Route("api/generate-description")]
    [ApiController]
    public class AIController : ControllerBase
    {
        private readonly string _geminiApiKey;
        private static readonly HttpClient _httpClient = new HttpClient();

        public AIController(IConfiguration configuration)
        {
            _geminiApiKey = configuration["GeminiApiKey"]; // 🔴 Secure API key storage
        }

        [HttpPost]
        public async Task<IActionResult> GenerateDescription([FromBody] UserInputModel input)
        {
            if (string.IsNullOrWhiteSpace(input.UserInput) || input.UserInput.Length < 5)
            {
                return BadRequest(new { message = "Please provide more details for a better response." });
            }

            var requestData = new
            {
                model = "gemini-pro",
                messages = new[]
                {
                    new { role = "user", content = $"Generate a professional event description: {input.UserInput}" }
                },
                max_tokens = 150
            };

            var requestBody = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateText?key={_geminiApiKey}"),
                Content = requestBody
            };

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { message = "AI service error.", details = errorContent });
            }

            var responseData = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
            var generatedText = responseData.GetProperty("candidates")[0].GetProperty("content").GetString();

            return Ok(new { generatedText });
        }
    }
}
