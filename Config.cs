using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiktatTool;

public class Config
{
    public static readonly string Path = System.IO.Path.Combine(
        AppContext.BaseDirectory, "config.json");

    [JsonPropertyName("GROQ_API_KEY")]
    public string GroqApiKey { get; set; } = "";

    [JsonPropertyName("HOTKEY")]
    public string Hotkey { get; set; } = "F9";

    [JsonPropertyName("AUTO_PASTE")]
    public bool AutoPaste { get; set; } = true;

    public static Config Load()
    {
        try
        {
            if (File.Exists(Path))
                return JsonSerializer.Deserialize<Config>(File.ReadAllText(Path)) ?? new Config();
        }
        catch { }
        return new Config();
    }

    public void Save()
    {
        File.WriteAllText(Path,
            JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
