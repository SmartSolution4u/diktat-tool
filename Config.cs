using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiktatTool;

public class DictationMode
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = "";

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;
}

public class Config
{
    public static readonly string FilePath = System.IO.Path.Combine(
        AppContext.BaseDirectory, "config.json");

    [JsonPropertyName("GROQ_API_KEY")]
    public string GroqApiKey { get; set; } = "";

    [JsonPropertyName("OPENROUTER_KEY")]
    public string OpenRouterKey { get; set; } = "";

    [JsonPropertyName("MODEL")]
    public string Model { get; set; } = "mistralai/mistral-small-3.1-24b-instruct";

    [JsonPropertyName("HOTKEY")]
    public string Hotkey { get; set; } = "F9";

    [JsonPropertyName("AUTO_PASTE")]
    public bool AutoPaste { get; set; } = true;

    [JsonPropertyName("ACTIVE_MODE")]
    public string ActiveMode { get; set; } = "Roh";

    [JsonPropertyName("MODES")]
    public List<DictationMode> Modes { get; set; } = DefaultModes();

    public static List<DictationMode> DefaultModes() => new()
    {
        new() { Name = "Roh",        Prompt = "",                                                                                                              Enabled = true },
        new() { Name = "Förmlich",   Prompt = "Formuliere diesen Text professionell und förmlich für eine geschäftliche E-Mail. Antworte NUR mit dem fertigen Text.",       Enabled = true },
        new() { Name = "Locker",     Prompt = "Formuliere diesen Text freundlich und locker, wie eine kurze Nachricht an einen Kollegen. Antworte NUR mit dem fertigen Text.", Enabled = true },
        new() { Name = "Korrigieren",Prompt = "Korrigiere nur Grammatik und Rechtschreibfehler, behalte den Stil bei. Antworte NUR mit dem korrigierten Text.",              Enabled = true },
        new() { Name = "Kürzer",     Prompt = "Fasse den Text prägnant zusammen, kürze auf das Wesentliche. Antworte NUR mit dem fertigen Text.",                           Enabled = true },
        new() { Name = "Aufzählung", Prompt = "Wandle den Text in eine übersichtliche Stichpunktliste um. Antworte NUR mit der fertigen Liste.",                            Enabled = true },
    };

    public DictationMode? GetActiveMode() =>
        Modes.FirstOrDefault(m => m.Name == ActiveMode) ?? Modes.FirstOrDefault();

    public static Config Load()
    {
        try
        {
            if (System.IO.File.Exists(FilePath))
            {
                var cfg = JsonSerializer.Deserialize<Config>(
                    System.IO.File.ReadAllText(FilePath)) ?? new Config();
                if (cfg.Modes == null || cfg.Modes.Count == 0)
                    cfg.Modes = DefaultModes();
                return cfg;
            }
        }
        catch { }
        return new Config();
    }

    public void Save()
    {
        System.IO.File.WriteAllText(FilePath,
            JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
