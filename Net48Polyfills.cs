#if NETFRAMEWORK
// Polyfills so modern C# features compile on .NET Framework 4.8 (Windows 7 build).
// This file compiles only for net48 — the .NET 10 build uses the real APIs.

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Runtime.CompilerServices
{
    // Enables records with init-only properties.
    internal static class IsExternalInit { }
}

namespace System
{
    // Enables s[..4] range syntax on net48.
    internal readonly struct Index
    {
        private readonly int _value;
        public Index(int value, bool fromEnd = false) { _value = fromEnd ? ~value : value; }
        public static Index FromStart(int value) { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); return new Index(value); }
        public static Index FromEnd(int value) { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); return new Index(value, true); }
        public int Value => _value < 0 ? ~_value : _value;
        public bool IsFromEnd => _value < 0;
        public int GetOffset(int length) => IsFromEnd ? length - Value : Value;
        public static implicit operator Index(int value) => FromStart(value);
        public override string ToString() => Value.ToString();
    }

    internal readonly struct Range
    {
        public Index Start { get; }
        public Index End { get; }
        public Range(Index start, Index end) { Start = start; End = end; }
        public static Range StartAt(Index start) => new Range(start, Index.FromEnd(0));
        public static Range EndAt(Index end) => new Range(Index.FromStart(0), end);
        public static Range All => new Range(Index.FromStart(0), Index.FromEnd(0));
        public override string ToString() => $"{Start}..{End}";
    }

    // WinForms source-generated helper (net6+); shim for net48.
    internal static class ApplicationConfiguration
    {
        public static void Initialize()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        }
    }
}

namespace System.Collections.Generic
{
    internal static class CollectionExtensions48
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dict, TKey key, TValue defaultValue = default!)
        {
            if (dict == null) throw new ArgumentNullException(nameof(dict));
            return dict.TryGetValue(key, out var v) ? v : defaultValue;
        }

        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
        {
            key = pair.Key;
            value = pair.Value;
        }
    }
}

namespace System.Linq
{
    internal static class LinqExtensions48
    {
        public static IEnumerable<TSource> TakeLast<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (count <= 0) return Enumerable.Empty<TSource>();
            var queue = new Queue<TSource>(count);
            foreach (var item in source)
            {
                if (queue.Count == count) queue.Dequeue();
                queue.Enqueue(item);
            }
            return queue;
        }

        public static IOrderedEnumerable<TSource> OrderDescending<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return source.OrderByDescending(x => x);
        }
    }
}

namespace System.Security.Cryptography
{
    // net48 lacks ECDsa.ImportSubjectPublicKeyInfo (SPKI import) — minimal DER parser
    // for our fixed P-256 (prime256v1) public key format.
    internal static class ECDsaExtensions48
    {
        public static void ImportSubjectPublicKeyInfo(this ECDsa ecdsa, byte[] data, out int bytesRead)
        {
            if (ecdsa == null) throw new ArgumentNullException(nameof(ecdsa));
            if (data == null) throw new ArgumentNullException(nameof(data));
            bytesRead = data.Length;

            // SPKI := SEQUENCE { SEQUENCE { OID ecPublicKey, OID prime256v1 }, BIT STRING point }
            var outer = ReadTlv(data, 0);
            if (outer.Tag != 0x30) throw new CryptographicException("Invalid SPKI: expected SEQUENCE.");

            var alg = ReadTlv(data, outer.ContentStart);
            if (alg.Tag != 0x30) throw new CryptographicException("Invalid SPKI: expected algorithm SEQUENCE.");

            var bits = ReadTlv(data, alg.Next);
            if (bits.Tag != 0x03) throw new CryptographicException("Invalid SPKI: expected BIT STRING.");

            // content: [0 unused-bits][0x04 uncompressed || X(32) || Y(32)]
            var c = bits.ContentStart;
            if (data[c] != 0) throw new CryptographicException("Invalid SPKI: unexpected unused bits.");
            if (bits.Len != 66 || data[c + 1] != 0x04)
                throw new CryptographicException("Invalid SPKI: expected P-256 uncompressed point.");

            var q = new ECPoint { X = new byte[32], Y = new byte[32] };
            Array.Copy(data, c + 2, q.X, 0, 32);
            Array.Copy(data, c + 34, q.Y, 0, 32);

            ecdsa.ImportParameters(new ECParameters
            {
                Curve = ECCurve.CreateFromOid(new Oid("1.2.840.10045.3.1.7")), // prime256v1 / P-256
                Q = q
            });
        }

        private readonly struct Tlv
        {
            public readonly byte Tag; public readonly int Len; public readonly int ContentStart; public readonly int Next;
            public Tlv(byte tag, int len, int contentStart) { Tag = tag; Len = len; ContentStart = contentStart; Next = contentStart + len; }
        }

        private static Tlv ReadTlv(byte[] d, int pos)
        {
            var tag = d[pos++];
            int len = d[pos++];
            if ((len & 0x80) != 0)
            {
                int n = len & 0x7F;
                len = 0;
                for (int i = 0; i < n; i++) len = (len << 8) | d[pos++];
            }
            return new Tlv(tag, len, pos);
        }
    }
}
#endif
