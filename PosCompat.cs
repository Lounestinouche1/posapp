namespace VectronPOS
{
    // Compatibility helpers used identically by both builds (net10 + net48).

    internal static class PosHex
    {
        public static string ToUpper(byte[] bytes)
        {
            const string hex = "0123456789ABCDEF";
            var sb = new System.Text.StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) { sb.Append(hex[b >> 4]); sb.Append(hex[b & 0xF]); }
            return sb.ToString();
        }

        public static byte[] From(string hex)
        {
            if (hex.Length % 2 != 0) throw new System.FormatException("Hex string must have an even length.");
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = System.Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return bytes;
        }
    }

    internal static class PosCrypto
    {
        public static byte[] Sha256(byte[] data)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return sha.ComputeHash(data);
        }
    }

    internal static class SafeRandom
    {
        private static readonly System.Random R = new System.Random();
        public static int Next(int minInclusive, int maxExclusive)
        {
            lock (R) return R.Next(minInclusive, maxExclusive);
        }
    }

    internal static class PosZip
    {
        // ExtractToDirectory with overwrite — 3-arg overload doesn't exist on net48.
        public static void ExtractOverwrite(string zipPath, string destinationDir)
        {
            System.IO.Directory.CreateDirectory(destinationDir);
            using var zip = System.IO.Compression.ZipFile.OpenRead(zipPath);
            foreach (var entry in zip.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.Combine(destinationDir, entry.FullName));
                    continue;
                }
                var destPath = System.IO.Path.Combine(destinationDir, entry.FullName);
                var dir = System.IO.Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
                System.IO.Compression.ZipFileExtensions.ExtractToFile(entry, destPath, overwrite: true);
            }
        }
    }
}
