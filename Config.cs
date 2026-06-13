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

    // Keys werden NICHT in die config.json geschrieben, sondern sicher im
    // Windows Credential Manager gespeichert (siehe SecretStore + Load/Save).
    [JsonIgnore]
    public string GroqApiKey { get; set; } = "";

    [JsonIgnore]
    public string OpenRouterKey { get; set; } = "";

    [JsonPropertyName("MODEL")]
    public string Model { get; set; } = "anthropic/claude-haiku-4.5";

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
        new() { Name = "Förmlich",   Prompt = "Formuliere den folgenden Text in professionelles, höfliches Business-Deutsch um. Gib AUSSCHLIESSLICH den umformulierten Fließtext zurück – OHNE Anrede (kein 'Sehr geehrte...'), OHNE Grußformel (kein 'Mit freundlichen Grüßen') und OHNE Signatur oder Platzhalter wie [Ihr Name] oder [Ihre Position]. Korrigiere Versprecher und Füllwörter.", Enabled = true },
        new() { Name = "Locker",     Prompt = "Formuliere den folgenden Text freundlich und locker um, wie eine kurze Nachricht an einen Kollegen. Gib NUR den Fließtext zurück – OHNE Anrede, OHNE Grußformel und OHNE Signatur oder Platzhalter.", Enabled = true },
        new() { Name = "Korrigieren",Prompt = "Korrigiere nur Grammatik und Rechtschreibfehler, behalte den Stil bei. Antworte NUR mit dem korrigierten Text.",              Enabled = true },
        new() { Name = "Kürzer",     Prompt = "Fasse den Text prägnant zusammen, kürze auf das Wesentliche. Antworte NUR mit dem fertigen Text.",                           Enabled = true },
        new() { Name = "Aufzählung", Prompt = "Wandle den Text in eine übersichtliche Stichpunktliste um. Antworte NUR mit der fertigen Liste.",                            Enabled = true },
    };

    public DictationMode? GetActiveMode() =>
        Modes.FirstOrDefault(m => m.Name == ActiveMode) ?? Modes.FirstOrDefault();

    public static Config Load()
    {
        Config cfg;
        string? rawJson = null;
        try
        {
            if (System.IO.File.Exists(FilePath))
            {
                rawJson = System.IO.File.ReadAllText(FilePath);
                cfg = JsonSerializer.Deserialize<Config>(rawJson) ?? new Config();
            }
            else cfg = new Config();
        }
        catch { cfg = new Config(); }

        // Modi aus der config.json uebernehmen; nur wenn keine vorhanden sind,
        // mit den Standard-Modi befuellen. So bleiben eigene Anpassungen des
        // Nutzers (Prompts, eigene Modi) erhalten.
        if (cfg.Modes == null || cfg.Modes.Count == 0)
            cfg.Modes = DefaultModes();

        // Keys aus dem Windows Credential Manager laden
        cfg.GroqApiKey    = SecretStore.Get("GROQ_API_KEY")   ?? "";
        cfg.OpenRouterKey = SecretStore.Get("OPENROUTER_KEY") ?? "";

        // Einmalige Migration alter Klartext-Keys aus der config.json
        if (rawJson != null)
            MigrateLegacyKeys(cfg, rawJson);

        return cfg;
    }

    /// <summary>
    /// Verschiebt im Klartext in config.json gespeicherte Keys in den
    /// Credential Manager und schreibt die config.json danach ohne Keys neu.
    /// </summary>
    private static void MigrateLegacyKeys(Config cfg, string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;
            bool hadLegacyKey = false;

            if (root.TryGetProperty("GROQ_API_KEY", out var g)
                && g.GetString() is { Length: > 0 } gk)
            {
                hadLegacyKey = true;
                if (string.IsNullOrEmpty(cfg.GroqApiKey))
                {
                    SecretStore.Set("GROQ_API_KEY", gk);
                    cfg.GroqApiKey = gk;
                }
            }
            if (root.TryGetProperty("OPENROUTER_KEY", out var o)
                && o.GetString() is { Length: > 0 } ok)
            {
                hadLegacyKey = true;
                if (string.IsNullOrEmpty(cfg.OpenRouterKey))
                {
                    SecretStore.Set("OPENROUTER_KEY", ok);
                    cfg.OpenRouterKey = ok;
                }
            }

            // Klartext-Keys aus der Datei entfernen (Save schreibt dank JsonIgnore ohne Keys)
            if (hadLegacyKey)
                cfg.Save();
        }
        catch { }
    }

    public void Save()
    {
        // API-Keys sicher im Credential Manager ablegen (nie im Klartext)
        if (!string.IsNullOrEmpty(GroqApiKey))    SecretStore.Set("GROQ_API_KEY", GroqApiKey);
        if (!string.IsNullOrEmpty(OpenRouterKey)) SecretStore.Set("OPENROUTER_KEY", OpenRouterKey);

        System.IO.File.WriteAllText(FilePath,
            JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }
}
