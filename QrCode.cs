using System.Text;

namespace VectronPOS;

internal static class QrCode
{
    private static readonly byte[] GfExp = new byte[512];
    private static readonly byte[] GfLog = new byte[256];

    static QrCode()
    {
        var x = 1;
        for (var i = 0; i < 255; i++)
        {
            GfExp[i] = (byte)x;
            GfLog[(byte)x] = (byte)i;
            x <<= 1;
            if ((x & 0x100) != 0) x ^= 0x11D;
        }
        for (var i = 255; i < 512; i++) GfExp[i] = GfExp[i - 255];
    }

    private static byte GfMul(byte a, byte b)
    {
        if (a == 0 || b == 0) return 0;
        return GfExp[GfLog[a] + GfLog[b]];
    }

    private static byte[] GeneratorPoly(int degree)
    {
        byte[] poly = [1];
        for (var i = 0; i < degree; i++)
        {
            var next = new byte[poly.Length + 1];
            for (var j = 0; j < poly.Length; j++)
            {
                next[j] ^= poly[j];
                next[j + 1] ^= GfMul(poly[j], GfExp[i]);
            }
            poly = next;
        }
        return poly;
    }

    private static byte[] RsEncode(IReadOnlyList<byte> data, int ecLen)
    {
        var gen = GeneratorPoly(ecLen);
        var res = new byte[ecLen];
        foreach (var b in data)
        {
            var factor = (byte)(b ^ res[0]);
            Array.Copy(res, 1, res, 0, ecLen - 1);
            res[ecLen - 1] = 0;
            if (factor != 0)
                for (var i = 0; i < ecLen; i++)
                    res[i] ^= GfMul(gen[i + 1], factor);
        }
        return res;
    }

    public static bool TryEncode(string text, out int size, out bool[,] modules)
    {
        size = 0;
        modules = null!;
        var data = Encoding.UTF8.GetBytes(text);
        int[] dataCw = [16, 28, 44, 64, 86];
        int[] ecCw = [10, 16, 26, 36, 48];
        var version = -1;
        for (var v = 1; v <= 5; v++)
        {
            if (data.Length * 8 + 4 + 8 <= dataCw[v - 1] * 8) { version = v; break; }
        }
        if (version < 0) return false;

        var bits = new List<bool>();
        void AddBits(int value, int count)
        {
            for (var i = count - 1; i >= 0; i--) bits.Add(((value >> i) & 1) != 0);
        }
        AddBits(0b0100, 4);
        AddBits(data.Length, 8);
        foreach (var b in data) AddBits(b, 8);
        var capBits = dataCw[version - 1] * 8;
        AddBits(0, Math.Min(4, capBits - bits.Count));
        while (bits.Count % 8 != 0) bits.Add(false);
        var codewords = new List<byte>();
        for (var i = 0; i < bits.Count; i += 8)
        {
            byte cw = 0;
            for (var j = 0; j < 8; j++) if (bits[i + j]) cw |= (byte)(1 << (7 - j));
            codewords.Add(cw);
        }
        byte[] pad = [0xEC, 0x11];
        var padIdx = 0;
        while (codewords.Count < dataCw[version - 1]) codewords.Add(pad[padIdx++ % 2]);
        var all = codewords.Concat(RsEncode(codewords, ecCw[version - 1])).ToArray();

        var sz = 4 * version + 17;
        var grid = new bool[sz, sz];
        var reserved = new bool[sz, sz];
        void SetFunc(int r, int c, bool dark)
        {
            if (r < 0 || c < 0 || r >= sz || c >= sz) return;
            grid[r, c] = dark;
            reserved[r, c] = true;
        }
        void Finder(int r0, int c0)
        {
            for (var r = -1; r <= 7; r++)
                for (var c = -1; c <= 7; c++)
                {
                    var rr = r0 + r;
                    var cc = c0 + c;
                    if (rr < 0 || cc < 0 || rr >= sz || cc >= sz) continue;
                    var ring = (r >= 0 && r <= 6 && (c == 0 || c == 6)) || (c >= 0 && c <= 6 && (r == 0 || r == 6));
                    var core = r >= 2 && r <= 4 && c >= 2 && c <= 4;
                    SetFunc(rr, cc, ring || core);
                }
        }
        Finder(0, 0);
        Finder(0, sz - 7);
        Finder(sz - 7, 0);
        for (var i = 8; i < sz - 8; i++)
        {
            SetFunc(6, i, i % 2 == 0);
            SetFunc(i, 6, i % 2 == 0);
        }
        if (version >= 2)
        {
            var ac = sz - 7;
            for (var r = -2; r <= 2; r++)
                for (var c = -2; c <= 2; c++)
                    SetFunc(ac + r, ac + c, Math.Max(Math.Abs(r), Math.Abs(c)) != 1);
        }
        SetFunc(4 * version + 9, 8, true);
        for (var i = 0; i <= 8; i++)
        {
            if (i != 6)
            {
                reserved[8, i] = true;
                reserved[i, 8] = true;
            }
        }
        for (var i = 0; i < 8; i++) reserved[8, sz - 1 - i] = true;
        for (var i = 0; i < 7; i++) reserved[sz - 1 - i, 8] = true;

        var idx = 0;
        var bitIdx = 0;
        bool NextBit()
        {
            if (idx >= all.Length) return false;
            var b = ((all[idx] >> (7 - bitIdx)) & 1) != 0;
            bitIdx++;
            if (bitIdx == 8) { bitIdx = 0; idx++; }
            return b;
        }
        var upward = true;
        for (var right = sz - 1; right >= 1; right -= 2)
        {
            if (right == 6) right = 5;
            for (var i = 0; i < sz; i++)
            {
                var row = upward ? sz - 1 - i : i;
                for (var k = 0; k < 2; k++)
                {
                    var col = right - k;
                    if (reserved[row, col]) continue;
                    var bit = NextBit();
                    if ((row + col) % 2 == 0) bit = !bit;
                    grid[row, col] = bit;
                }
            }
            upward = !upward;
        }

        var fmtData = 0b00 << 3;
        var rem = fmtData << 10;
        for (var i = 14; i >= 10; i--)
            if (((rem >> i) & 1) != 0) rem ^= 0x537 << (i - 10);
        var fmt = ((fmtData << 10) | (rem & 0x3FF)) ^ 0x5412;
        int F(int bit) => (fmt >> bit) & 1;
        grid[8, 0] = F(14) != 0;
        grid[8, 1] = F(13) != 0;
        grid[8, 2] = F(12) != 0;
        grid[8, 3] = F(11) != 0;
        grid[8, 4] = F(10) != 0;
        grid[8, 5] = F(9) != 0;
        grid[8, 7] = F(8) != 0;
        grid[8, 8] = F(7) != 0;
        grid[7, 8] = F(6) != 0;
        grid[5, 8] = F(5) != 0;
        grid[4, 8] = F(4) != 0;
        grid[3, 8] = F(3) != 0;
        grid[2, 8] = F(2) != 0;
        grid[1, 8] = F(1) != 0;
        grid[0, 8] = F(0) != 0;
        grid[sz - 1, 8] = F(14) != 0;
        grid[sz - 2, 8] = F(13) != 0;
        grid[sz - 3, 8] = F(12) != 0;
        grid[sz - 4, 8] = F(11) != 0;
        grid[sz - 5, 8] = F(10) != 0;
        grid[sz - 6, 8] = F(9) != 0;
        grid[sz - 7, 8] = F(8) != 0;
        grid[8, sz - 8] = F(7) != 0;
        grid[8, sz - 7] = F(6) != 0;
        grid[8, sz - 6] = F(5) != 0;
        grid[8, sz - 5] = F(4) != 0;
        grid[8, sz - 4] = F(3) != 0;
        grid[8, sz - 3] = F(2) != 0;
        grid[8, sz - 2] = F(1) != 0;
        grid[8, sz - 1] = F(0) != 0;
        size = sz;
        modules = grid;
        return true;
    }

    public static Bitmap ToBitmap(string text, int modulePx = 4, int quiet = 4)
    {
        if (!TryEncode(text, out var size, out var modules))
        {
            var fail = new Bitmap(Math.Max(1, text.Length * 6), 24);
            using var g = Graphics.FromImage(fail);
            g.Clear(Color.White);
            using var font = new Font("Segoe UI", 9F);
            g.DrawString(text, font, Brushes.Black, 0, 0);
            return fail;
        }
        var px = size + quiet * 2;
        var bmp = new Bitmap(px * modulePx, px * modulePx);
        using var gfx = Graphics.FromImage(bmp);
        gfx.Clear(Color.White);
        using var brush = new SolidBrush(Color.Black);
        for (var r = 0; r < size; r++)
            for (var c = 0; c < size; c++)
                if (modules[r, c])
                    gfx.FillRectangle(brush, (c + quiet) * modulePx, (r + quiet) * modulePx, modulePx, modulePx);
        return bmp;
    }
}
