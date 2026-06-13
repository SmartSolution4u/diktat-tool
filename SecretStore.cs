using System.Runtime.InteropServices;
using System.Text;

namespace DiktatTool;

/// <summary>
/// Speichert Geheimnisse (API-Keys) sicher im Windows Credential Manager
/// (Anmeldeinformationsverwaltung) statt im Klartext in der config.json.
/// Pendant zur keyring-Loesung der Python-Variante.
/// </summary>
public static class SecretStore
{
    private const string Prefix = "DiktatTool:";

    /// <summary>Liest ein Geheimnis; gibt null zurueck, wenn nicht vorhanden.</summary>
    public static string? Get(string name)
    {
        if (!CredRead(Prefix + name, CRED_TYPE_GENERIC, 0, out var handle))
            return null;
        try
        {
            var cred = Marshal.PtrToStructure<CREDENTIAL>(handle);
            if (cred.CredentialBlob == IntPtr.Zero || cred.CredentialBlobSize == 0)
                return string.Empty;
            var bytes = new byte[cred.CredentialBlobSize];
            Marshal.Copy(cred.CredentialBlob, bytes, 0, (int)cred.CredentialBlobSize);
            return Encoding.Unicode.GetString(bytes);
        }
        finally { CredFree(handle); }
    }

    /// <summary>Speichert ein Geheimnis sicher (an den Windows-Nutzer gebunden).</summary>
    public static void Set(string name, string value)
    {
        var blob = Encoding.Unicode.GetBytes(value ?? string.Empty);
        var blobPtr = Marshal.AllocHGlobal(Math.Max(blob.Length, 1));
        try
        {
            if (blob.Length > 0)
                Marshal.Copy(blob, 0, blobPtr, blob.Length);

            var cred = new CREDENTIAL
            {
                Type               = CRED_TYPE_GENERIC,
                TargetName         = Prefix + name,
                CredentialBlob     = blobPtr,
                CredentialBlobSize = (uint)blob.Length,
                Persist            = CRED_PERSIST_LOCAL_MACHINE,
                UserName           = Environment.UserName
            };
            if (!CredWrite(ref cred, 0))
                throw new InvalidOperationException(
                    $"CredWrite fehlgeschlagen (Win32-Fehler {Marshal.GetLastWin32Error()})");
        }
        finally { Marshal.FreeHGlobal(blobPtr); }
    }

    /// <summary>Loescht ein Geheimnis (ignoriert, wenn nicht vorhanden).</summary>
    public static void Delete(string name) => CredDelete(Prefix + name, CRED_TYPE_GENERIC, 0);

    // ── P/Invoke (advapi32) ───────────────────────────────────────────────────
    private const uint CRED_TYPE_GENERIC          = 1;
    private const uint CRED_PERSIST_LOCAL_MACHINE = 2;

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CredReadW")]
    private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CredWriteW")]
    private static extern bool CredWrite(ref CREDENTIAL credential, uint flags);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CredDeleteW")]
    private static extern bool CredDelete(string target, uint type, uint flags);

    [DllImport("advapi32.dll")]
    private static extern void CredFree(IntPtr buffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint    Flags;
        public uint    Type;
        public string  TargetName;
        public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint    CredentialBlobSize;
        public IntPtr  CredentialBlob;
        public uint    Persist;
        public uint    AttributeCount;
        public IntPtr  Attributes;
        public string? TargetAlias;
        public string  UserName;
    }
}
