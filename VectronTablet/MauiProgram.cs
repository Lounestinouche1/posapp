using Microsoft.Extensions.Logging;

namespace VectronTablet;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

#if ANDROID
		// P1.4 — flat disabled state. Android's AppCompat/Material button applies its own
		// grey disabled state (#C8C8C8) over any BackgroundColor we set, so the flat
		// Panel2 + dim-text "disabled" look never showed. Nulling the background tint
		// hands colour control back to MAUI so our flat colours render in every state.
		// Unconditional on purpose: the platform view may be MaterialButton or an
		// AppCompatButton depending on theme resolution, and BackgroundTintList lives on
		// the base View. No effects, no per-frame cost — a one-time mapper hook.
		Microsoft.Maui.Handlers.ButtonHandler.Mapper.AppendToMapping(
			"FlatNoDisabledTint",
			(Microsoft.Maui.Handlers.IButtonHandler h, Microsoft.Maui.IButton _) =>
			{
				if (h.PlatformView is Android.Views.View v)
					v.BackgroundTintList = null;
			});

		// P1.6 — flat Entry (Android). MAUI deliberately layers AndroidX's
		// abc_edit_text_material "line" ON TOP of our own background for every EditText
		// (ViewExtensions.UpdateBackground(EditText, IView): "Regardless of what the new
		// background is, we will need to apply the default background 'line' on top of
		// it"). That line is tinted by the theme accent, so every focused field showed a
		// purple underline — the last watch item from P1.5. Flat design has no line, so we
		// re-apply the entry's own fill as a plain colour (padding cached/restored around
		// the swap, the same guard MAUI uses — Android resets padding on background change)
		// and move focus feedback to a flat fill step: base colour mixed 15 % toward white
		// (Panel2 #243040 -> #454F5D, inside the existing flat ramp), applied from
		// FocusChange. Event-driven: no animation, no timers, no effects.
		Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(
			"VectronFlatEntry",
			(Microsoft.Maui.Handlers.IEntryHandler h, Microsoft.Maui.IEntry entry) =>
			{
				if (h.PlatformView is not Android.Widget.EditText et)
					return;

				// The fill MAUI renders for this entry (SolidPaint from BackgroundColor /
				// the implicit style), else transparent.
				var baseColor = (entry.Background as Microsoft.Maui.Graphics.SolidPaint)?.Color
					?? Microsoft.Maui.Graphics.Colors.Transparent;

				void Apply(Microsoft.Maui.Graphics.Color c)
				{
					var pl = et.PaddingLeft; var pt = et.PaddingTop;
					var pr = et.PaddingRight; var pb = et.PaddingBottom;
					et.SetBackgroundColor(ToArgb(c));
					et.SetPadding(pl, pt, pr, pb);
				}

				static Android.Graphics.Color ToArgb(Microsoft.Maui.Graphics.Color c) =>
					Android.Graphics.Color.Argb(
						(int)(c.Alpha * 255), (int)(c.Red * 255),
						(int)(c.Green * 255), (int)(c.Blue * 255));

				static Microsoft.Maui.Graphics.Color Lighten(Microsoft.Maui.Graphics.Color c, double t) =>
					Microsoft.Maui.Graphics.Color.FromRgb(
						c.Red + (1 - c.Red) * t, c.Green + (1 - c.Green) * t, c.Blue + (1 - c.Blue) * t);

				Apply(baseColor);

				// Flat focus affordance (replaces the removed Material line).
				if (baseColor.Alpha > 0)
					et.FocusChange += (_, e) =>
						Apply(e.HasFocus ? Lighten(baseColor, 0.15) : baseColor);
			});
#endif

		return builder.Build();
	}
}
