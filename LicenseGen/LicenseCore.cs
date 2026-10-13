using System.Security.Cryptography;
using System.Text;

namespace LicenseGen;

/// <summary>
/// Core license logic shared conceptually with the POS app.
/// The POS app only has the PUBLIC key (verify-only).
/// This generator holds the PRIVATE key (sign-only).
/// </summary>
public static class LicenseCore
{
    // ── ECDSA P-256 key pair (generate once, embed in both apps) ──
    // Private key: stays in LicenseGen only. Never ships to customers.
    // Public key:  embedded in VectronPOS for verification.
    private const string PrivateKeyB64 =
        "MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQg5Qo62bDlvC88y2AkwpeyghkH+H3dwxjl5OFf0lXA3xShRANCAATUHdYL4oc5NdT3b7ZSwBREPfZu0FckQIyXFM6PJOqGCXPmOoLU7jXw7Os8fvw2HdGqF9ptUV4laM3xk7iNWyvg";

    public const string PublicKeyB64 =
        "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE1B3WC+KHOTXU92+2UsAURD32btBXJECMlxTOjyTqhglz5jqC1O418OzrPH78Nh3RqhfabVFeJWjN8ZO4jVsr4A==";

    // Key format version
    private const byte KeyVersion = 1;

    // Expiry is stored as days since 2024-01-01 (fits uint32 until year ~2136)
    private static readonly DateTime Epoch = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // ── Key pair loading ──

    public static ECDsa LoadPrivateKey()
    {
        var bytes = Convert.FromBase64String(PrivateKeyB64);
        var ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(bytes, out _);
        return ecdsa;
    }

    public static ECDsa LoadPublicKey()
    {
        var bytes = Convert.FromBase64String(PublicKeyB64);
        var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(bytes, out _);
        return ecdsa;
    }

    // ── Machine fingerprint ──

    /// <summary>
    /// Computes a stable hardware fingerprint from CPU + motherboard + MAC + MachineGuid.
    /// Returns 8 bytes (64 bits) — enough to bind a key to one PC.
    /// </summary>
    public static byte[] ComputeFingerprint()
    {
        var sb = new StringBuilder();

        // CPU ID
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT ProcessorId FROM Win32_Processor");
            foreach (var obj in searcher.Get())
                sb.Append(obj["ProcessorId"]?.ToString());
        }
        catch { /* fallback below */ }

        // Motherboard serial
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT SerialNumber FROM Win32_BaseBoard");
            foreach (var obj in searcher.Get())
                sb.Append(obj["SerialNumber"]?.ToString());
        }
        catch { /* fallback below */ }

        // MAC address (first physical adapter)
        try
        {
            var macs = System.Net.NetworkInformation.NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up
                         && n.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                .Select(n => n.GetPhysicalAddress().ToString())
                .Where(m => !string.IsNullOrEmpty(m));
            foreach (var m in macs.Take(2)) sb.Append(m);
        }
        catch { /* fallback below */ }

        // Windows MachineGuid (most stable identifier)
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Cryptography");
            var guid = key?.GetValue("MachineGuid")?.ToString();
            if (!string.IsNullOrEmpty(guid)) sb.Append(guid);
        }
        catch { /* fallback below */ }

        // If everything failed, use machine name as last resort
        if (sb.Length == 0)
            sb.Append(Environment.MachineName);

        // Hash down to 8 bytes
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        var fp = new byte[8];
        Array.Copy(hash, fp, 8);
        return fp;
    }

    public static string FingerprintToRequestCode(byte[] fingerprint)
    {
        // Display fingerprint as a readable "request code"
        // Format: REQ-XXXXXXXX-XXXXXXXX (hex)
        var hex = Convert.ToHexString(fingerprint).ToUpperInvariant();
        return $"REQ-{hex[..4]}-{hex[4..8]}-{hex[8..12]}-{hex[12..16]}";
    }

    public static byte[] ParseRequestCode(string requestCode)
    {
        // Strip REQ- prefix and dashes, parse hex
        var clean = requestCode.Trim().ToUpperInvariant()
            .Replace("REQ-", "").Replace("-", "");
        if (clean.Length != 16)
            throw new FormatException("Invalid request code format. Expected: REQ-XXXX-XXXX-XXXX-XXXX");
        var bytes = new byte[8];
        for (int i = 0; i < 8; i++)
            bytes[i] = Convert.ToByte(clean.Substring(i * 2, 2), 16);
        return bytes;
    }

    // ── License key generation ──

    /// <summary>
    /// Generates a license key bound to a customer name, machine fingerprint, and expiry.
    /// </summary>
    public static string GenerateKey(string customerName, byte[] fingerprint, DateTime expiryUtc)
    {
        using var ecdsa = LoadPrivateKey();

        // Build payload:
        //   [0]     version (1 byte)
        //   [1..6]  customer name hash (6 bytes)
        //   [7..14] fingerprint (8 bytes)
        //   [15..18] expiry days since epoch (4 bytes, big-endian)
        var payload = new byte[19];
        payload[0] = KeyVersion;

        // Customer name hash (for display / cross-check, not secret)
        var nameHash = SHA256.HashData(Encoding.UTF8.GetBytes(customerName.Trim().ToUpperInvariant()));
        Array.Copy(nameHash, 0, payload, 1, 6);

        // Fingerprint
        Array.Copy(fingerprint, 0, payload, 7, 8);

        // Expiry in days
        var days = (uint)(expiryUtc.Date - Epoch).TotalDays;
        payload[15] = (byte)(days >> 24);
        payload[16] = (byte)(days >> 16);
        payload[17] = (byte)(days >> 8);
        payload[18] = (byte)days;

        // Sign payload
        var signature = ecdsa.SignData(payload, HashAlgorithmName.SHA256);

        // Combine payload + signature → encode
        var full = new byte[payload.Length + signature.Length];
        Array.Copy(payload, full, payload.Length);
        Array.Copy(signature, 0, full, payload.Length, signature.Length);

        var key = EncodeBase32(full);

        // Format: VECT-XXXX-XXXX-XXXX-... (groups of 8 after prefix)
        return FormatKey(key);
    }

    // ── Base32 encoding (RFC 4648, no padding) ──

    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    private static string EncodeBase32(byte[] data)
    {
        var result = new StringBuilder();
        int buffer = 0, bitsLeft = 0;
        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                result.Append(Base32Alphabet[(buffer >> (bitsLeft - 5)) & 31]);
                bitsLeft -= 5;
            }
        }
        if (bitsLeft > 0)
            result.Append(Base32Alphabet[(buffer << (5 - bitsLeft)) & 31]);
        return result.ToString();
    }

    private static string FormatKey(string base32)
    {
        // VECT-XXXXXXXX-XXXXXXXX-...
        var groups = new List<string>();
        for (int i = 0; i < base32.Length; i += 8)
        {
            var len = Math.Min(8, base32.Length - i);
            groups.Add(base32.Substring(i, len));
        }
        return "VECT-" + string.Join("-", groups);
    }

    // ── License info (for display in generator history) ──

    public record LicenseInfo(
        string CustomerName,
        string RequestCode,
        DateTime ExpiryUtc,
        DateTime GeneratedAt,
        string Key);

    // ── Local history persistence ──

    private static readonly string HistoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LicenseGen", "history.json");

    public static List<LicenseInfo> LoadHistory()
    {
        try
        {
            if (File.Exists(HistoryPath))
            {
                var json = File.ReadAllText(HistoryPath);
                return System.Text.Json.JsonSerializer.Deserialize<List<LicenseInfo>>(json) ?? new();
            }
        }
        catch { }
        return new();
    }

    public static void SaveHistory(List<LicenseInfo> history)
    {
        var dir = Path.GetDirectoryName(HistoryPath)!;
        Directory.CreateDirectory(dir);
        var json = System.Text.Json.JsonSerializer.Serialize(
            history, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(HistoryPath, json);
    }

    public static void AddToHistory(LicenseInfo info)
    {
        var history = LoadHistory();
        history.Insert(0, info);
        // Keep last 500 entries
        if (history.Count > 500) history.RemoveRange(500, history.Count - 500);
        SaveHistory(history);
    }
}
