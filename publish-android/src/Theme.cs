using System.Globalization;

namespace VectronTablet;

public enum ThemeMode { Dark, Light }
public enum GraphicsTier { High, Lite }

/// <summary>
/// Token + appearance layer for the tablet (PLAN §3, DESIGN.md "Appearance system").
/// Mirrors the Windows <c>Skin</c> class: every colour the pages use is a resource key
/// written by <see cref="Apply"/>, so a theme switch never touches page code.
/// Theme = Dark/Light (Dark default), Graphics = Auto/High/Lite (Auto default).
/// </summary>
public static class Theme
{
    private static ResourceDictionary? Res => Application.Current?.Resources;

    public static ThemeMode Mode { get; private set; } = ThemeMode.Dark;
    public static GraphicsTier Quality { get; private set; } = GraphicsTier.Lite;
    /// <summary>The persisted choice: "auto" | "high" | "lite".</summary>
    public static string QualityChoice { get; private set; } = "auto";
    /// <summary>Why the tier resolved the way it did (shown in the Appearance sheet).</summary>
    public static string QualityReason { get; private set; } = "Lite — not evaluated";
    public static bool IsHigh => Quality == GraphicsTier.High;
    public static bool IsLight => Mode == ThemeMode.Light;

    public static Color C(string key) =>
        Res != null && Res.TryGetValue(key, out var v) && v is Color c ? c : Colors.Gray;

    public static Style S(string key) =>
        Res != null && Res.TryGetValue(key, out var v) && v is Style s ? s : new Style(typeof(VisualElement));

    // ── palettes ───────────────────────────────────────────────────────────
    // Dark keeps the shipped brand identity (navy + teal). Light is its own
    // contrast-checked palette — never an inversion (DESIGN.md).
    private static readonly (string Key, string Dark, string Light)[] Palette =
    [
        ("Bg",           "#0F141A", "#F3F6FA"),
        ("Panel",        "#1A222C", "#FFFFFF"),
        ("Panel2",       "#243040", "#E8ECF3"),
        ("Kpi",          "#1C2432", "#EDF1F6"),
        ("Mint",         "#70C1B3", "#0E7C63"),
        ("MintDeep",     "#5AA99B", "#0A6552"),
        ("Ink",          "#0F141A", "#FFFFFF"),
        ("Amber",        "#F4B860", "#96600A"),
        ("Danger",       "#E07A5F", "#B03A2E"),
        ("Green",        "#4E9F6E", "#1F6B45"),
        ("Txt",          "#E9EFF5", "#131821"),
        ("TxtDim",       "#8C99A8", "#586476"),
        ("HudLine",      "#2E3A4B", "#D6DDE7"),
        ("ReadyBg",      "#16321F", "#E4F3E8"),
        ("TakenBg",      "#332A16", "#FBF0DA"),
        ("PlusBg",       "#9600DC96", "#B30E7C63"),
        ("EightySixBg",  "#CC3E3230", "#CCEDE2E0"),
        ("DangerInk",    "#F06A5A", "#B03A2E"),
        ("Price",        "#47D6AE", "#0E7C63"),
        // Text drawn on a saturated fill (badges, status pills) — white in both themes,
        // the tablet counterpart of the Windows Skin.OnFill.
        ("OnFill",       "#FFFFFF", "#FFFFFF")
    ];

    /// <summary>Reads the persisted choices, resolves the tier and writes every palette key.
    /// MUST run before any page is parsed (called from the App constructor) so the XAML
    /// <c>StaticResource</c> lookups pick up the active palette.</summary>
    public static void Apply()
    {
        Mode = string.Equals(Preferences.Default.Get("theme", "dark"), "light", StringComparison.OrdinalIgnoreCase)
            ? ThemeMode.Light : ThemeMode.Dark;
        QualityChoice = Preferences.Default.Get("graphics", "auto");
        (Quality, QualityReason) = Resolve(QualityChoice);

        var res = Res;
        if (res == null) return;
        foreach (var (key, dark, light) in Palette)
            res[key] = Color.FromArgb(IsLight ? light : dark);

        // Native chrome (action sheets, alerts, status bar icons) follows the app theme.
        // System dialogs stay platform-native — we only tell the platform which side it is.
        if (Application.Current != null)
            Application.Current.UserAppTheme = IsLight ? AppTheme.Light : AppTheme.Dark;
#if ANDROID
        // The activity is NOT recreated for a UiMode change, so the system bars have to be
        // recoloured explicitly after every switch.
        MainActivity.ApplySystemBars();
#endif

        // Style-level colours that live in App.xaml. Styles are parsed ONCE (at App.xaml load),
        // so a setter that captured a palette colour keeps that Color instance forever —
        // replacing the dictionary entry never reaches it. Every themed setter is therefore
        // re-pointed at the active palette here, by style key (the list is short and explicit;
        // add a line when a themed style is added).
        SetStyleColor("PrimaryBtn", Button.BackgroundColorProperty, C("Mint"));
        SetStyleColor("PrimaryBtn", Button.TextColorProperty, C("Ink"));
        SetStyleColor("GhostBtn", Button.BackgroundColorProperty, C("Panel2"));
        SetStyleColor("GhostBtn", Button.TextColorProperty, C("Txt"));
        SetStyleColor("TableBtn", Button.BackgroundColorProperty, C("Panel"));
        SetStyleColor("TableBtn", Button.TextColorProperty, C("Txt"));
        SetStyleColor("ChipBtn", Button.BackgroundColorProperty, C("Kpi"));
        SetStyleColor("ChipBtn", Button.TextColorProperty, C("TxtDim"));
        SetStyleColor("SectionLbl", Label.TextColorProperty, C("Txt"));
        SetStyleColor("DimLbl", Label.TextColorProperty, C("TxtDim"));
    }

    /// <summary>Rewrites one setter of a shared style (styles are parsed once, so their
    /// StaticResource colours would otherwise keep the palette they were created with).</summary>
    private static void SetStyleColor(string styleKey, BindableProperty property, Color value)
    {
        if (Res?.TryGetValue(styleKey, out var raw) != true || raw is not Style style) return;
        var existing = style.Setters.FirstOrDefault(s => s.Property == property);
        if (existing != null) existing.Value = value;
        else style.Setters.Add(new Setter { Property = property, Value = value });
    }

    /// <summary>Auto resolves to High only on a capable device; Lite is the flat baseline.</summary>
    private static (GraphicsTier Tier, string Reason) Resolve(string choice)
    {
        switch ((choice ?? "auto").Trim().ToLowerInvariant())
        {
            case "high": return (GraphicsTier.High, "High — chosen manually");
            case "lite": return (GraphicsTier.Lite, "Lite — chosen manually");
        }
        long ramMb;
        int cores;
        try { ramMb = MemoryMb(); } catch { ramMb = 0; }
        try { cores = Environment.ProcessorCount; } catch { cores = 1; }
        if (ramMb < 3800) return (GraphicsTier.Lite, $"Lite — {ramMb / 1024.0:0.#} GB RAM (needs 4 GB)");
        if (cores < 2) return (GraphicsTier.Lite, $"Lite — {cores} CPU core (needs 2)");
        return (GraphicsTier.High, $"High — {ramMb / 1024.0:0.#} GB RAM, {cores} cores");
    }

    private static long MemoryMb()
    {
        try
        {
            using var reader = new Java.IO.RandomAccessFile("/proc/meminfo", "r");
            var line = reader.ReadLine() ?? "";
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 && long.TryParse(parts[1], out var kb) ? kb / 1024 : 0;
        }
        catch { return 0; }
    }

    /// <summary>Persists + applies a theme, then rebuilds the shell so every open page
    /// re-reads the tokens (the tablet equivalent of the Windows restart prompt).</summary>
    public static void SetTheme(string theme)
    {
        Preferences.Default.Set("theme", theme);
        Apply();
        Rebuild();
    }

    public static void ApplyTheme(string theme, string motion = "")
    {
        SetTheme(theme);
    }

    /// <summary>Persists + applies a graphics choice (auto|high|lite).</summary>
    public static void SetQuality(string choice)
    {
        Preferences.Default.Set("graphics", choice);
        Apply();
        Rebuild();
    }

    private static void Rebuild()
    {
        // XAML resolves a StaticResource ONCE, when the page is parsed, and keeps that
        // Color instance forever. Rewriting the dictionary (Apply) therefore never
        // reaches an already-open page: verified on device, switching Dark -> Light left
        // the page background, app bar and hairlines dark while every code-built row
        // (room chips, table cards) turned light. One tree walk re-points them.
        Restyle();
        if (Services.AppState.IsLoggedIn) App.ShowMain();
        else App.ShowLogin();
    }

    /// <summary>
    /// Rewrites the palette colours already resolved into the live visual tree so an
    /// open page follows a theme switch. Pure property assignment on exact palette
    /// matches - no timers, no layout pass of its own, nothing to do on Lite vs High.
    /// </summary>
    public static void Restyle()
    {
        var map = Swap();
        if (map.Count == 0) return;
        var windows = Application.Current?.Windows;
        if (windows == null) return;
        foreach (var w in windows)
        {
            Walk(w.Page, map);
            var modals = w.Page?.Navigation?.ModalStack;
            if (modals != null)
                foreach (var m in modals) Walk(m, map);
        }
    }

    /// <summary>old palette colour -> active palette colour, same role.</summary>
    private static List<(Color From, Color To)> Swap()
    {
        var list = new List<(Color From, Color To)>(Palette.Length);
        foreach (var (_, dark, light) in Palette)
        {
            var from = Color.FromArgb(IsLight ? dark : light);
            var to = Color.FromArgb(IsLight ? light : dark);
            if (!from.Equals(to)) list.Add((from, to));
        }
        return list;
    }

    private static Color? Map(Color? c, List<(Color From, Color To)> map)
    {
        if (c == null) return null;
        foreach (var (from, to) in map)
            if (from.Equals(c)) return to;
        return c;
    }

    private static void Walk(IVisualTreeElement? node, List<(Color From, Color To)> map)
    {
        if (node == null) return;
        if (node is VisualElement v)
        {
            v.BackgroundColor = Map(v.BackgroundColor, map);
            switch (v)
            {
                case Label l: l.TextColor = Map(l.TextColor, map); break;
                case Button b:
                    b.TextColor = Map(b.TextColor, map);
                    b.BorderColor = Map(b.BorderColor, map);
                    break;
                case Entry en:
                    en.TextColor = Map(en.TextColor, map);
                    en.PlaceholderColor = Map(en.PlaceholderColor, map);
                    break;
                case Border bd when bd.Stroke is SolidColorBrush sb:
                    var stroke = Map(sb.Color, map);
                    if (stroke != null && !stroke.Equals(sb.Color)) bd.Stroke = new SolidColorBrush(stroke);
                    break;
            }
        }
        foreach (var child in node.GetVisualChildren()) Walk(child, map);
    }

    // ── High-tier helpers (all no-ops on Lite) ─────────────────────────────
    /// <summary>One-shot reveal: a 150 ms fade + 12 px slide. Lite paints the final frame
    /// immediately — no timers, no animation loop.</summary>
    public static void Reveal(View view, int rise = 12)
    {
        if (!IsHigh || view == null) return;
        view.Opacity = 0;
        view.TranslationY = rise;
        _ = view.FadeTo(1, 150, Easing.CubicOut);
        _ = view.TranslateTo(0, 0, 150, Easing.CubicOut);
    }

    /// <summary>Soft layered depth under a surface (High only; Lite keeps the flat 1px edge).</summary>
    public static void Depth(Border border)
    {
        if (!IsHigh || border == null) return;
        border.Shadow = new Shadow
        {
            Brush = new SolidColorBrush(Color.FromArgb(IsLight ? "#22000000" : "#66000000")),
            Offset = new Point(0, 3),
            Radius = 10,
            Opacity = 0.9f
        };
    }

    /// <summary>Press step: lighter on the dark palette, darker on the light one, so the
    /// feedback is always visible without breaking text contrast.</summary>
    public static Color Press(Color c) => IsLight ? Darken(c, 0.10) : Lighten(c, 0.10);

    /// <summary>Flat pressed step (P1.4): base colour mixed a touch toward white while held.
    /// Pure colour maths, no animation, no timer — safe on low-spec tablets.</summary>
    public static Color Lighten(Color c, double t) => Color.FromRgb(
        (byte)(c.Red * 255 + (255 - c.Red * 255) * t),
        (byte)(c.Green * 255 + (255 - c.Green * 255) * t),
        (byte)(c.Blue * 255 + (255 - c.Blue * 255) * t));

    /// <summary>Mix toward black (used for the light-palette press step).</summary>
    public static Color Darken(Color c, double t) => Color.FromRgb(
        (byte)(c.Red * 255 * (1 - t)),
        (byte)(c.Green * 255 * (1 - t)),
        (byte)(c.Blue * 255 * (1 - t)));

    private static GridLength GL(string s) => s switch
    {
        "Auto" => GridLength.Auto,
        "*" => GridLength.Star,
        _ when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) => new GridLength(d),
        _ => GridLength.Star
    };

    public static ColumnDefinitionCollection Cols(params string[] specs)
    {
        var col = new ColumnDefinitionCollection();
        foreach (var s in specs)
            foreach (var part in s.Split(','))
                col.Add(new ColumnDefinition { Width = GL(part.Trim()) });
        return col;
    }

    /// <summary>P1.4 — flat pressed feedback for a Button. A VisualStateManager Pressed state is a
    /// no-op on MAUI Android (verified on device), so we bind the pointer events directly (the same
    /// pattern already proven on the category chips). Reads the live base colour at press time and
    /// restores it on release, so it plays fine with callers that repaint the button (active chips,
    /// the send button's enabled/disabled states). No-ops when the button is disabled.
    ///
    /// P1.5 FIX (regression found while verifying the qty stepper): attaching ANY gesture
    /// recognizer on MAUI Android makes the view consume its touch stream, so the native click
    /// never reaches the platform listener and Button.Clicked stays dead (verified on device:
    /// the qty "+"/"−" and the back arrow lightened on press but their Clicked never fired,
    /// while an un-bound "×" button clicked fine). A gesture recognizer and a platform click
    /// are therefore mutually exclusive on MAUI Android — so a PressBind'ed button MUST pass
    /// its click work through <paramref name="onClick"/> and declare no Clicked handler; the
    /// recognizer's PointerReleased is its single click path. That is exactly how MAUI's own
    /// TapGestureRecognizer works (a managed lambda from a pointer event), so nothing here
    /// touches platform interop. NOT done via the platform view's Java PerformClick: verified
    /// on device that re-performing the native click from our code (synchronously or posted one
    /// main-thread turn later) hard-crashed the app whenever the handler rebuilt the visual
    /// tree around the button mid-click (basket qty rows).
    ///
    /// Drag-off semantics: an "armed" flag set at press and cleared by PointerExited means a
    /// finger that slides off the button before release does not click — matching native.
    /// <paramref name="canClick"/> gates the press itself (no light-up, no arm) for a button
    /// that is painted-disabled without IsEnabled=false (the flat-disabled rule from P1.4).</summary>
    public static void PressBind(Button b, Action? onClick = null, Func<bool>? canClick = null)
    {
        var hasNormal = false;
        var normal = Colors.Transparent;
        var armed = false;
        var press = new PointerGestureRecognizer();
        press.PointerPressed += (_, _) =>
        {
            if (!b.IsEnabled || (canClick != null && !canClick())) return;
            normal = b.BackgroundColor;
            hasNormal = true;
            armed = true;
            b.BackgroundColor = Press(normal);
        };
        void Release()
        {
            if (!hasNormal) return;
            b.BackgroundColor = normal;
            hasNormal = false;
        }
        press.PointerExited += (_, _) => { armed = false; Release(); };
        press.PointerReleased += (_, _) =>
        {
            var fire = armed;
            armed = false;
            Release();
            if (!fire || !b.IsEnabled || (canClick != null && !canClick())) return;
            // DEFERRED one main-thread turn on purpose (a one-shot dispatch post — not a
            // timer): a Clicked handler is allowed to remove its own view (the basket qty
            // rows rebuild on every change), but doing that while this very view's pointer
            // dispatch is still on the stack hard-crashed the app on device (verified with
            // three variants: direct call, Java PerformClick, and PerformClick posted a turn
            // later — the last one because the Java frame still outlived the teardown).
            // Posting first lets the touch dispatch fully unwind, so the handler then runs
            // exactly like the proven-safe native path (an un-bound "×" button tears its row
            // down from a plain click without issue, and the name-tap action sheet does it
            // after its await). The re-check guards state that changed during the turn.
            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!b.IsEnabled || (canClick != null && !canClick())) return;
                onClick?.Invoke();
            });
        };
        b.GestureRecognizers.Add(press);
    }
}
