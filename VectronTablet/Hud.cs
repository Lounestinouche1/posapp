using Microsoft.Maui.Graphics;

namespace VectronTablet;

/// <summary>
/// P1.7 Flat-HUD (DESIGN.md §Flat-HUD layer) — the Android mirror of the Windows
/// <c>Hud.Brackets</c>/<c>HudPanel</c>: four 1px L-shaped corner marks (optionally over a
/// 1px edge hairline) for the signature surfaces (login card, order bar).
///
/// Static geometry: drawn once per invalidate (first layout / size change) — no animation,
/// no glow, no per-frame cost; the low-spec rules (PLAN §3) ban the expensive HUD effects.
/// Drawn through a <see cref="GraphicsView"/> with <c>InputTransparent=True</c> on top of the
/// surface, so it never intercepts touches (the buttons below keep working).
/// </summary>
public sealed class HudFrameDrawable : IDrawable
{
    /// <summary>Draw the 1px edge rect too — off when the surface already has a Stroke.</summary>
    public bool DrawEdge { get; set; }

    /// <summary>Arm length of each L-mark, DIP (Windows: 18 on card + order panel).</summary>
    public float Arm { get; set; } = 18f;

    /// <summary>Corner inset, DIP (Windows: 5 on the card, 6 on the order panel).</summary>
    public float Inset { get; set; } = 5f;

    public void Draw(ICanvas canvas, RectF rect)
    {
        if (DrawEdge)
        {
            canvas.StrokeColor = Theme.C("HudLine");
            canvas.StrokeSize = 1f;
            canvas.DrawRectangle(rect.X + 0.5f, rect.Y + 0.5f, rect.Width - 1f, rect.Height - 1f);
        }

        canvas.StrokeColor = Theme.C("Mint");
        canvas.StrokeSize = 1f;
        float x0 = rect.X + Inset, y0 = rect.Y + Inset;
        float x1 = rect.Right - Inset, y1 = rect.Bottom - Inset;
        canvas.DrawLine(x0, y0, x0 + Arm, y0); canvas.DrawLine(x0, y0, x0, y0 + Arm);   // top-left
        canvas.DrawLine(x1 - Arm, y0, x1, y0); canvas.DrawLine(x1, y0, x1, y0 + Arm);   // top-right
        canvas.DrawLine(x0, y1, x0 + Arm, y1); canvas.DrawLine(x0, y1 - Arm, x0, y1);   // bottom-left
        canvas.DrawLine(x1 - Arm, y1, x1, y1); canvas.DrawLine(x1, y1 - Arm, x1, y1);   // bottom-right
    }
}

/// <summary>
/// P1.9 — the Android mirror of the Windows <c>VectronMark</c>: a mint ring with a rounded
/// chevron inside, plus the four HUD corner brackets. Pure static vector geometry (no image,
/// no timer): the High-tier reveal animates the whole view's opacity/offset, not the drawable.
/// </summary>
public sealed class VectronMarkDrawable : IDrawable
{
    public void Draw(ICanvas canvas, RectF rect)
    {
        var size = Math.Min(rect.Width, rect.Height);
        if (size < 8) return;
        var r = new RectF(rect.X + (rect.Width - size) / 2, rect.Y + (rect.Height - size) / 2, size - 1, size - 1);
        var cx = r.Center.X;
        var cy = r.Center.Y;
        var accent = Theme.C("Mint");

        canvas.StrokeColor = accent;
        canvas.StrokeSize = Math.Max(1.6f, size / 20f);
        canvas.DrawEllipse(r);

        canvas.StrokeSize = Math.Max(2.2f, size / 11f);
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;
        var path = new PathF();
        path.MoveTo(cx - size * 0.24f, cy - size * 0.20f);
        path.LineTo(cx, cy + size * 0.26f);
        path.LineTo(cx + size * 0.24f, cy - size * 0.20f);
        canvas.DrawPath(path);

        canvas.StrokeColor = Theme.C("MintDeep");
        canvas.StrokeSize = 1f;
        float arm = Math.Max(4f, size / 6f), i = 1f;
        float x0 = r.X + i, y0 = r.Y + i, x1 = r.Right - i, y1 = r.Bottom - i;
        canvas.DrawLine(x0, y0, x0 + arm, y0); canvas.DrawLine(x0, y0, x0, y0 + arm);
        canvas.DrawLine(x1 - arm, y0, x1, y0); canvas.DrawLine(x1, y0, x1, y0 + arm);
        canvas.DrawLine(x0, y1, x0 + arm, y1); canvas.DrawLine(x0, y1 - arm, x0, y1);
        canvas.DrawLine(x1 - arm, y1, x1, y1); canvas.DrawLine(x1, y1 - arm, x1, y1);
    }
}
