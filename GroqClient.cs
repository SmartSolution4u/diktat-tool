using System.Net.Http.Headers;
using System.Text.Json;

namespace DiktatTool;

public class GroqClient
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly string _apiKey;

    public GroqClient(string apiKey) => _apiKey = apiKey;

    public async Task<string> TranscribeAsync(string wavPath)
    {
        var bytes = await File.ReadAllBytesAsync(wavPath);

        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        form.Add(file, "file", "audio.wav");
        form.Add(new StringContent("whisper-large-v3"), "model");
        form.Add(new StringContent("de"), "language");

        var req = new HttpRequestMessage(HttpMethod.Post,
            "https://api.groq.com/openai/v1/audio/transcriptions") { Content = form };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var resp = await _http.SendAsync(req);
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("text").GetString()?.Trim() ?? "";
    }
}
