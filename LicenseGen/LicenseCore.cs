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

    // ── CSV database ──
    // Two outputs on every generation:
    //   1. clients/<ClientName>.csv  — one file per client (all renewals stacked)
    //   2. clients/_all-clients.csv  — master database, sorted by client name
    // Files use ';' separator + UTF-8 BOM so Excel opens one column per field.
    // Saved in a "clients" folder next to LicenseGen.exe (moves with the tool).
    // Falls back to %APPDATA% if the exe folder is not writable (e.g. Program Files).

    public static readonly string ClientsDir = ResolveClientsDir();

    private static string ResolveClientsDir()
    {
        try
        {
            var local = Path.Combine(AppContext.BaseDirectory, "clients");
            Directory.CreateDirectory(local);
            // Write probe — ensure we actually have permission
            var probe = Path.Combine(local, ".probe");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);

            // One-time migration: move old AppData database next to the exe
            var oldDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LicenseGen", "clients");
            if (Directory.Exists(oldDir) &&
                !Directory.EnumerateFiles(local, "*.csv").Any() &&
                Directory.EnumerateFiles(oldDir, "*.csv").Any())
            {
                foreach (var f in Directory.EnumerateFiles(oldDir, "*.csv"))
                    File.Copy(f, Path.Combine(local, Path.GetFileName(f)));
            }
            return local;
        }
        catch
        {
            var fallback = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LicenseGen", "clients");
            Directory.CreateDirectory(fallback);
            return fallback;
        }
    }

    private static readonly string MasterCsv = Path.Combine(ClientsDir, "_all-clients.csv");

    private const string CsvHeader =
        "Client Name;Phone;Request Code;PC Name;Expiry Date;Generated At;License Key;Notes";

    private static readonly UTF8Encoding CsvEncoding = new(true); // UTF-8 with BOM (Excel-friendly)

    /// <summary>A single row in the client database.</summary>
    public record ClientRecord(
        string CustomerName,
        string Phone,
        string RequestCode,
        string PcName,
        DateTime ExpiryUtc,
        DateTime GeneratedAt,
        string Key,
        string Notes = "");

    /// <summary>
    /// Saves a new license record to both the per-client CSV and the master CSV.
    /// Master file is re-sorted alphabetically by client name after each save.
    /// </summary>
    public static void SaveToCsv(ClientRecord record)
    {
        Directory.CreateDirectory(ClientsDir);

        // 1. Per-client file (chronological)
        var safeName = SanitizeFileName(record.CustomerName);
        var clientFile = Path.Combine(ClientsDir, safeName + ".csv");
        if (File.Exists(clientFile))
        {
            // Migrate legacy comma-separated files to new ';' + Phone format
            var existingHeader = File.ReadLines(clientFile, CsvEncoding).FirstOrDefault() ?? "";
            if (!existingHeader.Contains("Phone", StringComparison.OrdinalIgnoreCase))
            {
                var legacy = LoadCsvFile(clientFile);
                var sb0 = new StringBuilder(CsvHeader + "\n");
                foreach (var r in legacy) sb0.Append(CsvRow(r)).Append('\n');
                File.WriteAllText(clientFile, sb0.ToString(), CsvEncoding);
            }
        }
        else
        {
            File.WriteAllText(clientFile, CsvHeader + "\n", CsvEncoding);
        }
        File.AppendAllText(clientFile, CsvRow(record) + "\n", CsvEncoding);

        // 2. Master database — reload all, add new row, sort, rewrite
        var all = LoadMasterCsv();
        all.RemoveAll(r =>
            r.CustomerName == record.CustomerName &&
            r.RequestCode == record.RequestCode &&
            r.GeneratedAt == record.GeneratedAt &&
            r.Key == record.Key);
        all.Add(record);
        var sorted = all
            .OrderBy(r => r.CustomerName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.GeneratedAt)
            .ToList();

        var sb = new StringBuilder(CsvHeader + "\n");
        foreach (var r in sorted) sb.Append(CsvRow(r)).Append('\n');
        File.WriteAllText(MasterCsv, sb.ToString(), CsvEncoding);
    }

    /// <summary>
    /// Loads a client's records from their CSV file.
    /// Returns empty list if the file doesn't exist.
    /// </summary>
    public static List<ClientRecord> LoadClientCsv(string customerName)
    {
        var safeName = SanitizeFileName(customerName);
        var file = Path.Combine(ClientsDir, safeName + ".csv");
        return LoadCsvFile(file);
    }

    /// <summary>Loads the master database of all clients.</summary>
    public static List<ClientRecord> LoadMasterCsv()
    {
        return LoadCsvFile(MasterCsv);
    }

    /// <summary>Loads any CSV file (for the Import button). Handles ';' and old ',' files.</summary>
    public static List<ClientRecord> LoadCsvFile(string path)
    {
        var result = new List<ClientRecord>();
        try
        {
            if (!File.Exists(path)) return result;
            var lines = File.ReadAllLines(path, CsvEncoding);
            if (lines.Length == 0) return result;

            // Detect delimiter + format from header
            var header = lines[0];
            char delim = header.Contains(';') ? ';' : ',';
            bool hasPhoneCol = header.Contains("Phone", StringComparison.OrdinalIgnoreCase);

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var rec = ParseCsvLine(lines[i], delim, hasPhoneCol);
                if (rec != null) result.Add(rec);
            }
        }
        catch { }
        return result;
    }

    private static string CsvRow(ClientRecord r)
    {
        static string Esc(string s)
        {
            if (s.Contains(';') || s.Contains('"') || s.Contains('\n'))
                return '"' + s.Replace("\"", "\"\"") + '"';
            return s;
        }
        return string.Join(";",
            Esc(r.CustomerName),
            Esc(r.Phone),
            Esc(r.RequestCode),
            Esc(r.PcName),
            r.ExpiryUtc.ToString("yyyy-MM-dd"),
            r.GeneratedAt.ToString("yyyy-MM-dd HH:mm"),
            Esc(r.Key),
            Esc(r.Notes));
    }

    private static ClientRecord? ParseCsvLine(string line, char delim, bool hasPhoneCol)
    {
        try
        {
            var f = ParseCsvFields(line, delim);
            if (!hasPhoneCol)
            {
                // Legacy format: Customer;Request;PC;Expiry;Generated;Key;Notes (7 cols, no phone)
                if (f.Length < 6) return null;
                return new ClientRecord(f[0], "", f[1], f[2],
                    DateTime.Parse(f[3]), DateTime.Parse(f[4]), f[5],
                    f.Length > 6 ? f[6] : "");
            }
            if (f.Length < 7) return null;
            return new ClientRecord(f[0], f[1], f[2], f[3],
                DateTime.Parse(f[4]), DateTime.Parse(f[5]), f[6],
                f.Length > 7 ? f[7] : "");
        }
        catch { return null; }
    }

    private static string[] ParseCsvFields(string line, char delim)
    {
        var fields = new List<string>();
        var cur = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; }
                    else inQuotes = false;
                }
                else cur.Append(c);
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == delim) { fields.Add(cur.ToString()); cur.Clear(); }
                else cur.Append(c);
            }
        }
        fields.Add(cur.ToString());
        return fields.ToArray();
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = name.Trim();
        foreach (var c in invalid) clean = clean.Replace(c, '_');
        return clean;
    }
}
