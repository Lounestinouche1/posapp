using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace VectronPOS;

/// <summary>
/// License validation for Vectron POS.
/// Uses ECDSA P-256 public-key verification — cannot forge keys without the private key
/// (which only lives in LicenseGen, never ships with the POS app).
/// </summary>
public static class License
{
    // ── Public key (paired with LicenseGen's private key) ──
    // This is SAFE to embed — verification only, cannot sign.
    private const string PublicKeyB64 =
        "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE1B3WC+KHOTXU92+2UsAURD32btBXJECMlxTOjyTqhglz5jqC1O418OzrPH78Nh3RqhfabVFeJWjN8ZO4jVsr4A==";

    private const byte KeyVersion = 1;
    private static readonly DateTime Epoch = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Grace period after expiry — app still works but shows a warning.</summary>
    public static readonly TimeSpan GracePeriod = TimeSpan.FromDays(7);

    // ── License status ──

    public enum Status
    {
        Valid,          // within expiry
        GracePeriod,    // expired but within grace — warning banner
        Expired,        // past grace — blocks sales
        Invalid,        // key doesn't match this machine or is corrupt
        NotActivated    // no key entered yet
    }

    public record LicenseInfo(
        Status Status,
        string? CustomerName,
        DateTime? ExpiryUtc,
        string? Key,
        string? RequestCode,
        TimeSpan? TimeRemaining);

    // ── Machine fingerprint (same algorithm as LicenseGen) ──

    public static byte[] ComputeFingerprint()
    {
        var sb = new StringBuilder();

        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT ProcessorId FROM Win32_Processor");
            foreach (var obj in searcher.Get())
                sb.Append(obj["ProcessorId"]?.ToString());
        }
        catch { }

        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT SerialNumber FROM Win32_BaseBoard");
            foreach (var obj in searcher.Get())
                sb.Append(obj["SerialNumber"]?.ToString());
        }
        catch { }

        try
        {
            var macs = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                         && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(n => n.GetPhysicalAddress().ToString())
                .Where(m => !string.IsNullOrEmpty(m));
            foreach (var m in macs.Take(2)) sb.Append(m);
        }
        catch { }

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            var guid = key?.GetValue("MachineGuid")?.ToString();
            if (!string.IsNullOrEmpty(guid)) sb.Append(guid);
        }
        catch { }

        if (sb.Length == 0) sb.Append(Environment.MachineName);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        var fp = new byte[8];
        Array.Copy(hash, fp, 8);
        return fp;
    }

    public static string GetRequestCode()
    {
        var fp = ComputeFingerprint();
        var hex = Convert.ToHexString(fp).ToUpperInvariant();
        return $"REQ-{hex[..4]}-{hex[4..8]}-{hex[8..12]}-{hex[12..16]}";
    }

    // ── Key parsing & verification ──

    private static byte[] DecodeBase32(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var clean = input.ToUpperInvariant().Replace("-", "").Replace(" ", "");
        var result = new List<byte>();
        int buffer = 0, bitsLeft = 0;
        foreach (var c in clean)
        {
            int idx = alphabet.IndexOf(c);
            if (idx < 0) continue; // skip non-alphabet chars
            buffer = (buffer << 5) | idx;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                result.Add((byte)((buffer >> (bitsLeft - 8)) & 0xFF));
                bitsLeft -= 8;
            }
        }
        return result.ToArray();
    }

    /// <summary>
    /// Parses and verifies a license key.
    /// Returns null if valid, or an error message if invalid.
    /// </summary>
    public static (bool valid, LicenseInfo info) VerifyAndParse(string key)
    {
        try
        {
            // Strip prefix and decode
            var clean = key.Trim().ToUpperInvariant();
            if (clean.StartsWith("VECT-")) clean = clean[5..];
            var data = DecodeBase32(clean);

            // Minimum: 19 payload + 64 signature = 83 bytes
            if (data.Length < 83)
                return (false, new LicenseInfo(Status.Invalid, null, null, null, null, null));

            // Split payload / signature
            var payload = new byte[19];
            var signature = new byte[data.Length - 19];
            Array.Copy(data, 0, payload, 0, 19);
            Array.Copy(data, 19, signature, 0, signature.Length);

            // Verify signature with public key
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(PublicKeyB64), out _);
            if (!ecdsa.VerifyData(payload, signature, HashAlgorithmName.SHA256))
                return (false, new LicenseInfo(Status.Invalid, null, null, null, null, null));

            // Check version
            if (payload[0] != KeyVersion)
                return (false, new LicenseInfo(Status.Invalid, null, null, null, null, null));

            // Extract fingerprint
            var keyFp = new byte[8];
            Array.Copy(payload, 7, keyFp, 0, 8);

            // Compare with THIS machine's fingerprint
            var localFp = ComputeFingerprint();
            if (!keyFp.SequenceEqual(localFp))
                return (false, new LicenseInfo(Status.Invalid, null, null, null, null, null));

            // Extract expiry
            uint days = (uint)((payload[15] << 24) | (payload[16] << 16) | (payload[17] << 8) | payload[18]);
            var expiry = Epoch.AddDays(days);

            // Anti-tamper: check clock hasn't gone backwards
            var storedLastSeen = LoadLastSeen();
            var now = DateTime.UtcNow;
            if (storedLastSeen.HasValue && now < storedLastSeen.Value)
                return (false, new LicenseInfo(Status.Invalid, null, null, null, null, null));
            SaveLastSeen(now);

            // Determine status
            Status status;
            TimeSpan? remaining;
            if (now <= expiry)
            {
                status = Status.Valid;
                remaining = expiry - now;
            }
            else if (now <= expiry + GracePeriod)
            {
                status = Status.GracePeriod;
                remaining = expiry + GracePeriod - now;
            }
            else
            {
                status = Status.Expired;
                remaining = null;
            }

            return (true, new LicenseInfo(status, null, expiry, key.Trim(), null, remaining));
        }
        catch
        {
            return (false, new LicenseInfo(Status.Invalid, null, null, null, null, null));
        }
    }

    // ── Local storage ──

    private static string LicensePath => Path.Combine(
        AppContext.BaseDirectory, "client database", "license.dat");

    private static string LastSeenPath => Path.Combine(
        AppContext.BaseDirectory, "client database", "license.time");

    public static void SaveLicense(string key, string requestCode)
    {
        var dir = Path.GetDirectoryName(LicensePath)!;
        Directory.CreateDirectory(dir);
        // Store: requestCode + newline + key (simple, protected by file perms)
        File.WriteAllText(LicensePath, $"{requestCode}\n{key}");
    }

    public static (string? requestCode, string? key) LoadLicense()
    {
        try
        {
            if (File.Exists(LicensePath))
            {
                var lines = File.ReadAllLines(LicensePath);
                if (lines.Length >= 2)
                    return (lines[0], lines[1]);
            }
        }
        catch { }
        return (null, null);
    }

    private static void SaveLastSeen(DateTime dt)
    {
        try
        {
            var dir = Path.GetDirectoryName(LastSeenPath)!;
            Directory.CreateDirectory(dir);
            File.WriteAllText(LastSeenPath, dt.Ticks.ToString());
        }
        catch { }
    }

    private static DateTime? LoadLastSeen()
    {
        try
        {
            if (File.Exists(LastSeenPath))
            {
                var ticks = long.TryParse(File.ReadAllText(LastSeenPath).Trim(), out var t) ? t : 0;
                if (ticks > 0) return new DateTime(ticks, DateTimeKind.Utc);
            }
        }
        catch { }
        return null;
    }

    // ── High-level API for the app ──

    public static LicenseInfo GetStatus()
    {
        var (requestCode, key) = LoadLicense();
        if (string.IsNullOrEmpty(key))
            return new LicenseInfo(Status.NotActivated, null, null, null, requestCode, null);

        var (valid, info) = VerifyAndParse(key);
        if (!valid)
            return info with { Status = Status.Invalid };

        return info with { RequestCode = requestCode };
    }

    public static bool IsActive => GetStatus().Status is Status.Valid or Status.GracePeriod;
}
