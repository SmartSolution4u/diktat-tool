using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DiktatTool;

public class OpenRouterClient
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly string _apiKey;

    public OpenRouterClient(string apiKey) => _apiKey = apiKey;

    public async Task<string> ReformulateAsync(string text, string prompt, string model)
    {
        var body = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = prompt },
                new { role = "user",   content = text   }
            },
            max_tokens = 1000
        };

        var req = new HttpRequestMessage(HttpMethod.Post,
            "https://openrouter.ai/api/v1/chat/completions")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        req.Headers.Add("HTTP-Referer", "https://github.com/SmartSolution4u/diktat-tool");

        var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()?.Trim() ?? text;
    }
}
