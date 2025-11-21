using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TaskManager.DTOs;

namespace TaskManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AIController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        public AIController(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        [HttpPost("generate")]
        public async Task GenerateText([FromBody] AIRequestDto request)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:streamGenerateContent?key={apiKey}";
            var systemIntruction = "You are an AI writing assistant embedded in a Notion-clone app. " +
                                    "Your task is to modify the provided text based on the user's command " +
                                    "(e.g., summarize, fix grammar, translate, continue writing). " +
                                    "Return ONLY the result text, no markdown formatting unless requested, no pleasantries.";
            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = $"Context: {request.Context}\n\nCommand: {request.Command}" }
                        }
                    }
                },
            };
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Post, url) { Content = content},
                HttpCompletionOption.ResponseHeadersRead
            );
            if(!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                Response.StatusCode = (int)response.StatusCode;
                await Response.WriteAsync($"Gemini API error: {errorResponse}");
                return;
            }
            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            string line;
            while((line = await reader.ReadLineAsync()) != null)
            {
                if(line.Contains("\"text\": \""))
                {
                    var parts = line.Split(new[] { "\"text\": \"" }, StringSplitOptions.None);
                    if(parts.Length > 1)
                    {
                        var textSegment = parts[1].Split('"')[0];
                        textSegment = System.Text.RegularExpressions.Regex.Unescape(textSegment);

                        var data = JsonSerializer.Serialize(new { text = textSegment });
                        await Response.WriteAsync($"data: {data}\n\n");
                        await Response.Body.FlushAsync();
                    }
                }
            }
            await Response.WriteAsync("data: [DONE]\n\n");
            await Response.Body.FlushAsync();
        }
    }
}
