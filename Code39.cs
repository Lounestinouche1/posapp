using System.Drawing;

namespace VectronPOS;

internal static class Code39
{
    // Two wide bars among the 5 bars (bar weights 1,2,4,7,0 → pair sum = value 1..10; 4+7=11 is value 10)
    private static readonly int[][] BarPairs =
    [
        [],
        [0, 4],
        [1, 4],
        [0, 1],
        [2, 4],
        [0, 2],
        [1, 2],
        [3, 4],
        [0, 3],
        [1, 3],
        [2, 3],
    ];

    private static int? CharValue(char c)
    {
        if (c >= '0' && c <= '9') return c == '0' ? 10 : c - '0';
        if (c >= 'A' && c <= 'Z')
        {
            var i = c - 'A';
            if (i < 10) return i + 1;
            if (i < 20) return i - 9;
            return i - 19;
        }
        return c switch
        {
            '-' => 7,
            '.' => 8,
            ' ' => 9,
            '*' => 10,
            _ => null
        };
    }

    // Wide-space position: 0 = S1 (U-Z, -, '.', space, '*'), 1 = S2 (digits), 2 = S3 (A-J), 3 = S4 (K-T)
    private static int Group(char c)
    {
        if (c >= '0' && c <= '9') return 1;
        if (c >= 'A' && c <= 'J') return 2;
        if (c >= 'K' && c <= 'T') return 3;
        return 0;
    }

    public static bool CanEncode(string text) =>
        !string.IsNullOrEmpty(text) && text.ToUpperInvariant().All(c => CharValue(c) != null);

    public static Bitmap Render(string text, int module = 3)
    {
        var s = ("*" + text.ToUpperInvariant().Replace("*", "") + "*");
        var widths = new List<int>();
        for (var ci = 0; ci < s.Length; ci++)
        {
            var c = s[ci];
            var v = CharValue(c) ?? 10;
            var wideBars = BarPairs[v];
            var group = Group(c);
            for (var b = 0; b < 5; b++)
            {
                widths.Add(wideBars.Contains(b) ? 3 : 1);       // bar
                if (b < 4) widths.Add(b == group ? 3 : 1);      // space
            }
            if (ci < s.Length - 1) widths.Add(1);               // inter-character gap
        }
        var quiet = 10;
        var totalModules = quiet * 2 + widths.Sum();
        var height = module * 30;
        var bmp = new Bitmap(Math.Max(1, totalModules * module), height);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.White);
        using var black = new SolidBrush(Color.Black);
        var x = quiet * module;
        for (var i = 0; i < widths.Count; i++)
        {
            var w = widths[i] * module;
            if (i % 2 == 0) g.FillRectangle(black, x, 0, w, height - module * 6);
            x += w;
        }
        return bmp;
    }
}
