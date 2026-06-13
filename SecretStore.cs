using System.Security.Cryptography;
using System.Text;

namespace DiktatTool;

/// <summary>
/// Speichert Geheimnisse (API-Keys) mit Windows-DPAPI verschluesselt in
/// kleinen Dateien neben der config.json. Die Verschluesselung ist an den
/// Windows-Benutzer gebunden (kein Klartext, nicht auf andere PCs uebertragbar)
/// und funktioniert OHNE den Credential-Manager-Dienst (VaultSvc).
/// </summary>
public static class SecretStore
{
    // Zusaetzliche Entropie — koppelt die Verschluesselung an diese Anwendung.
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("DiktatTool-secrets-v1");

    private static string DirPath =>
        System.IO.Path.GetDirectoryName(Config.FilePath) ?? AppContext.BaseDirectory;

    private static string PathFor(string name) =>
        System.IO.Path.Combine(DirPath, $"secret_{name}.dat");

    /// <summary>Liest ein Geheimnis; null wenn nicht vorhanden oder nicht lesbar.</summary>
    public static string? Get(string name)
    {
        try
        {
            var path = PathFor(name);
            if (!System.IO.File.Exists(path)) return null;
            var enc = System.IO.File.ReadAllBytes(path);
            if (enc.Length == 0) return null;
            var dec = ProtectedData.Unprotect(enc, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(dec);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Speichert ein Geheimnis DPAPI-verschluesselt (an den Nutzer gebunden).</summary>
    public static void Set(string name, string value)
    {
        try
        {
            var plain = Encoding.UTF8.GetBytes(value ?? string.Empty);
            var enc   = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);
            System.IO.File.WriteAllBytes(PathFor(name), enc);
        }
        catch
        {
            // Persistierung fehlgeschlagen — Key gilt dann nur fuer die laufende Sitzung.
        }
    }

    /// <summary>Loescht ein gespeichertes Geheimnis (ignoriert, wenn nicht vorhanden).</summary>
    public static void Delete(string name)
    {
        try
        {
            var path = PathFor(name);
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
        catch
        {
        }
    }
}
