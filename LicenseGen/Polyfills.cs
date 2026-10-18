namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

namespace System.Security.Cryptography
{
    // net48 lacks ECDsa.ImportPkcs8PrivateKey / ImportSubjectPublicKeyInfo —
    // minimal DER parsers for our fixed P-256 key formats.
    internal static class ECDsaLicenseGenExtensions
    {
        public static void ImportSubjectPublicKeyInfo(this ECDsa ecdsa, byte[] data, out int bytesRead)
        {
            if (ecdsa == null) throw new ArgumentNullException(nameof(ecdsa));
            if (data == null) throw new ArgumentNullException(nameof(data));
            bytesRead = data.Length;

            var outer = ReadTlv(data, 0);
            if (outer.Tag != 0x30) throw new CryptographicException("Invalid SPKI: expected SEQUENCE.");

            var alg = ReadTlv(data, outer.ContentStart);
            if (alg.Tag != 0x30) throw new CryptographicException("Invalid SPKI: expected algorithm SEQUENCE.");

            var bits = ReadTlv(data, alg.Next);
            if (bits.Tag != 0x03) throw new CryptographicException("Invalid SPKI: expected BIT STRING.");

            var c = bits.ContentStart;
            if (data[c] != 0) throw new CryptographicException("Invalid SPKI: unexpected unused bits.");
            if (bits.Len != 66 || data[c + 1] != 0x04)
                throw new CryptographicException("Invalid SPKI: expected P-256 uncompressed point.");

            var q = new ECPoint { X = new byte[32], Y = new byte[32] };
            Array.Copy(data, c + 2, q.X, 0, 32);
            Array.Copy(data, c + 34, q.Y, 0, 32);

            ecdsa.ImportParameters(new ECParameters
            {
                Curve = ECCurve.CreateFromOid(new Oid("1.2.840.10045.3.1.7")),
                Q = q
            });
        }

        public static void ImportPkcs8PrivateKey(this ECDsa ecdsa, byte[] data, out int bytesRead)
        {
            if (ecdsa == null) throw new ArgumentNullException(nameof(ecdsa));
            if (data == null) throw new ArgumentNullException(nameof(data));
            bytesRead = data.Length;

            // PKCS#8 := SEQUENCE { INTEGER 0, SEQUENCE { OID ecPublicKey, OID prime256v1 },
            //                      OCTET STRING { ECPrivateKey (RFC 5915) } }
            var outer = ReadTlv(data, 0);
            if (outer.Tag != 0x30) throw new CryptographicException("Invalid PKCS8: expected SEQUENCE.");

            var ver = ReadTlv(data, outer.ContentStart);
            if (ver.Tag != 0x02) throw new CryptographicException("Invalid PKCS8: expected INTEGER.");

            var alg = ReadTlv(data, ver.Next);
            if (alg.Tag != 0x30) throw new CryptographicException("Invalid PKCS8: expected algorithm SEQUENCE.");

            var oct = ReadTlv(data, alg.Next);
            if (oct.Tag != 0x04) throw new CryptographicException("Invalid PKCS8: expected OCTET STRING.");

            // ECPrivateKey := SEQUENCE { INTEGER 1, OCTET STRING privkey,
            //                            [0]{ OID prime256v1 }?, [1]{ BIT STRING point }? }
            var inner = ReadTlv(data, oct.ContentStart);
            if (inner.Tag != 0x30) throw new CryptographicException("Invalid ECPrivateKey: expected SEQUENCE.");

            var kVer = ReadTlv(data, inner.ContentStart);
            if (kVer.Tag != 0x02) throw new CryptographicException("Invalid ECPrivateKey: expected INTEGER.");

            var kD = ReadTlv(data, kVer.Next);
            if (kD.Tag != 0x04 || kD.Len != 32)
                throw new CryptographicException("Invalid ECPrivateKey: expected 32-byte private key.");

            var d = new byte[32];
            Array.Copy(data, kD.ContentStart, d, 0, 32);

            // optional [0] params, optional [1] public key — scan to tag 0xA1
            ECPoint? q = null;
            var pos = kD.Next;
            var stop = oct.Next;
            while (pos < stop)
            {
                var tlv = ReadTlv(data, pos);
                if (tlv.Tag == 0xA1)
                {
                    var bits = ReadTlv(data, tlv.ContentStart);
                    if (bits.Tag != 0x03) throw new CryptographicException("Invalid ECPrivateKey: [1] not BIT STRING.");
                    var c = bits.ContentStart;
                    if (bits.Len != 66 || data[c] != 0 || data[c + 1] != 0x04)
                        throw new CryptographicException("Invalid ECPrivateKey: expected P-256 uncompressed point.");
                    var x = new byte[32]; var y = new byte[32];
                    Array.Copy(data, c + 2, x, 0, 32);
                    Array.Copy(data, c + 34, y, 0, 32);
                    q = new ECPoint { X = x, Y = y };
                }
                pos = tlv.Next;
            }

            if (q == null)
                throw new CryptographicException("Invalid ECPrivateKey: public key missing.");

            ecdsa.ImportParameters(new ECParameters
            {
                Curve = ECCurve.CreateFromOid(new Oid("1.2.840.10045.3.1.7")),
                D = d,
                Q = q.Value
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
