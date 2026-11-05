---
version: alpha
name: Vectron POS Suite
description: Design language for the Vectron POS product suite — the Windows operator terminal and the Android waiter companion app.
colors:
  background-windows: "#131821"
  panel-windows: "#1B222E"
  panel-light-windows: "#252F3E"
  sidebar-windows: "#171D28"
  mint-windows: "#47D6AE"
  border-windows: "#2E3A4B"
  mint-pressed-windows: "#35B891"
  appbar-windows: "#0F141C"
  appbar-border-windows: "#1F2734"
  kpi-windows: "#1C2432"
  card-border-windows: "#263040"
  nav-active-windows: "#2F4C52"
  light-background-windows: "#F3F6FA"
  light-panel-windows: "#FFFFFF"
  light-panel-light-windows: "#E8ECF3"
  light-appbar-windows: "#FFFFFF"
  light-kpi-windows: "#EDF1F6"
  light-card-border-windows: "#D6DDE7"
  light-border-windows: "#C8D1DC"
  light-nav-active-windows: "#DBE8E7"
  light-text-windows: "#131821"
  light-text-dim-windows: "#586476"
  light-accent-windows: "#0E7C63"
  light-accent-pressed-windows: "#0A6552"
  background-android: "#0F141A"
  panel-android: "#1A222C"
  panel-2-android: "#243040"
  mint-android: "#70C1B3"
  amber-android: "#F4B860"
  danger-android: "#E07A5F"
  green-android: "#4E9F6E"
  txt-android: "#E9EFF5"
  txt-dim-android: "#8C99A8"
typography:
  windows:
    fontFamily: Segoe UI
  android:
    fontFamily: OpenSansRegular
---

## Overview

Vectron POS is a restaurant point-of-sale suite: a Windows Forms operator terminal (Windows 7 and Windows 10 builds) and an Android waiter companion app (Vectron Waiter, .NET MAUI) that order at the table against the terminal's REST API. Both surfaces share one design direction: dark slate backgrounds, a single mint/teal accent that carries every primary action and price, elevated panels for content groupings, and warm tinted bars reserved for modal states. The two platforms use their own exact palette values (recorded separately below) but the same role system — background, panel, panel-light, mint accent, dim secondary text.

## Colors

One accent color per platform carries primary actions, active states, and prices; it is never used as a large background. Panel tones step lighter than the background to group content; the Windows sidebar is its own tone, darker than the panels but distinct from the background.

Status and warning states on Android use amber (warnings), danger (destructive/86'd), and green (confirmed) — the same roles the Windows terminal paints as full-width tinted status bars.

## Typography

Windows uses Segoe UI with its Semibold companion for headings, totals, and the brand mark; sizes are set per surface (brand and totals scale with window width). Android uses OpenSansRegular throughout. Both platforms keep secondary information (dates, hints, connection status) visually recessed through color rather than size.

## Components

- **Operator login** (Windows): a centered card (`#1B222E` panel with a 1px `#2E3A4B` border) floats on the dark background; the mint brand mark is centered at the top with a short mint rule beneath it, a dim centered subtitle below. Fields follow a fixed vertical rhythm (dim label → 4px → 36px field → 20px gap). The focused field's fill steps one tone lighter (`#252F3E` → `#2E3A4B`) — flat, event-driven, no animation. The full-width mint action button (48px, dark text) darkens to the pressed mint `#35B891` while the pointer is down. The password field takes keyboard focus when the dialog opens; Enter submits.
- **Product tile** (both platforms): the product image fills the full tile width and roughly 80% of the tile height; the product name sits underneath (wrapping to at most two lines so long names stay readable) with the price on its own line below it in the mint accent. A circular `+` badge overlays the top-right of the image; out-of-stock ("86'd") products replace it with an `86` badge, strike through the name, and color the price with the danger tone. Products without an image render the accent block in the product's assigned color. Tiles are tap/click targets and open the product's modifier flow.
- **Category chips** (both platforms): a horizontal row above the product grid; the active category is filled with the mint accent and the inactive ones use the panel tone.
- **Status bars** (Windows): modal states (QR pairing, guest ordering, training mode, license warning) are announced by a full-width tinted bar pinned under the header with a white label, never by changing the page background.
- **Current-order panel** (both platforms): a persistent side panel listing order lines, the operator and table in dim text, and the total in a large semibold face; the primary payment/send action is the only mint-filled button in the panel.

## Flat-HUD layer (P1.7)

The "futuristic" layer: a flat interpretation of the HUD / Sci-Fi FUI style (ui-ux-pro-max style 51) applied on top of the existing base — same navy + mint palette, no rebrand. It borrows only that style's **free geometry** (thin lines, decorative brackets, technical markers) and explicitly drops its cost:moderate drivers (glow, scan animations, ticker text, blur), which the low-spec rules (PLAN §3) already ban. Every element is static, drawn once per paint (state change or resize) — zero runtime cost.

- **Corner brackets**: 1px L-shaped marks at the four corners of signature surfaces (operator-login card, current-order panel), inset 4–6px with a 16–18px arm, in mint over the `#2E3A4B` edge. This is the identity motif of the layer.
- **Hairlines**: 1px `#2E3A4B` rules used as a structural grid — under the brand mark, under the header, above the totals block. Always exactly 1px, never animated.
- **Accent rails**: 3px mint vertical bars used as markers — left of the greeting in the header, on the left edge of the active sidebar nav item, 3×16px tick before the current-order title.
- **LED status dot**: the sidebar footer shows `●` as its own label so the dot alone carries state color (mint healthy / amber `#F4B860` failed) while the text stays dim; footer text is uppercased (HUD micro-copy; case-insensitive languages unaffected).
- **Outline category chips** (Windows): inactive chips drop their solid panel fill for a 1px `#2E3A4B` outline on the background — technical "segmented control" look; the active chip stays a solid mint fill. Hover/pressed keep the existing flat fill steps.
- **Focus underline**: a 2px mint rail appears under the focused login field, event-driven (GotFocus/LostFocus), alongside the existing fill step.

Uppercasing is applied only to in-app strings via `ToUpper()` (no new translatable keys; Arabic is case-neutral). Button labels are never case-changed or letter-spaced — fixed-size buttons must not clip translations (break-ui lesson).

> **Superseded by P1.8 for the main window:** the left sidebar, its LED footer and the nav rail no longer exist (the shell is now a top app bar). The HUD motifs that survive are the hairlines, the accent rails, the corner brackets on dialogs and the outline-segment look, now expressed as pills.

## Appearance system (P1.8)

Two user settings, persisted per install and mirrored on both platforms: **Theme** (Dark default / Light) and **Graphics** (Auto default / High / Lite). `Auto` resolves to High only when RAM ≥ 4 GB, ≥ 2 logical cores, not a Remote Desktop session and **not Windows 7**; everything else gets Lite. Settings → Appearance shows the resolved tier plus the reason.

### The two palettes

Every colour is a `Skin.*` role (Windows) / `App.xaml` key (Android); no screen may hardcode a value. Light is a **first-class palette**, not an inversion.

| Role | Dark | Light |
|---|---|---|
| Background | `#131821` | `#F3F6FA` |
| App bar | `#0F141C` | `#FFFFFF` |
| App bar hairline | `#1F2734` | `#DFE5ED` |
| Panel / card | `#1B222E` | `#FFFFFF` |
| Panel light | `#252F3E` | `#E8ECF3` |
| KPI / quiet pill | `#1C2432` | `#EDF1F6` |
| Card border | `#263040` | `#D6DDE7` |
| Border (inputs) | `#2E3A4B` | `#C8D1DC` |
| Nav pill active | `#2F4C52` | `#DBE8E7` |
| Text | `#FFFFFF` | `#131821` |
| Text dim | `#8F9AAB` | `#586476` |
| Accent (mint) | `#47D6AE` | `#0E7C63` |
| Accent pressed | `#35B891` | `#0A6552` |
| Ink on accent | `#131821` | `#FFFFFF` |

The mint `#47D6AE` fails 4.5:1 on white, so Light uses the deep green `#0E7C63` (≈ 5.2:1 on `#FFFFFF`, ≈ 4.7:1 on `#F3F6FA`) and switches the ink on accent to white. Text on saturated status fills is always `OnFill` (white) in both themes.

### Graphics tiers

| | **Lite** (low-spec / Win7 / RDP) | **High** (modern PC) |
|---|---|---|
| Fills | solid colour only | 2-stop vertical gradient (+6% light at the top) on app bar, cards, tiles, pills |
| Depth | 1px hairline edges | inner highlight + translucent shade bands under cards/dialogs (2 rects, no blur) |
| Motion | none — instant | one shared 150 ms timer for reveals (logo draw-in, card slide) |
| Cost | identical to the pre-P1.8 baseline | one extra paint per state change, idle cost unchanged |

Lite must remain pixel- and cost-identical to the old flat look; every effect sits behind `Skin.IsHigh`.

### New shell (P1.8)

The window is no longer "sidebar + header + content". It is:

1. **Top app bar** (72 px, `Skin.AppBar`, 1px `AppBarBorder` hairline at its bottom). Left: the vector `VectronMark` (mint ring + rounded chevron + the four HUD corner brackets) and a two-line wordmark lockup — `VECTRON` in `Skin.Text` semibold, `P O S` micro-copy in the accent. Then four nav **pills** (Order / Rooms / Operator / Settings, radius 18, 36 px tall): inactive = transparent with `PillHover` on hover, active = `NavActive` fill with accent text. Right cluster, in order: status LED + `MAIN PC` micro-copy (IP / print-server detail in the tooltip), the **sales KPI chip** (caption `TODAY` + amount, radius 10, `Kpi` fill, accent amount, gradient + depth on High), the operator chip (radius 18, `Kpi` fill, `CardBorder` edge) and a `⋯` overflow pill that opens a `ContextMenuStrip` rendered with the skin (`SkinColorTable`) for the nine secondary screens (Clock, Stock, Kitchen, 86 board, Bookings, Schedule, Gift cards, Purchases, Cash drawer — each gated by feature + permission).
2. **Page head** (88 px): 3 px mint rail, greeting, date, 1px bottom hairline.
3. **Workspace**: the product grid on the left (category **pills**, radius 17, active = mint fill / inactive = `Kpi`), and a **floating order card** on the right — radius 14, `CardBorder` edge, depth on High, 18 px gap from the grid, content inset 20 px.
4. **Detail pages** (`ShowView`): same head language — mint tick, 17 pt title, `Kpi` back pill on the right, hairline under.

Entrance motion (High only): the mark draws itself in (ring sweep + chevron reach, 420 ms) and the order card slides 26 px into place (220 ms), both on `Shown`, one shared timer, cancelled with the form.

The nav pill for the current section stays lit while its detail pages are open (Settings keeps its pill lit inside the settings hub, Operator inside the operator picker); other detail pages clear it.

## Appearance on Android (P1.9)

The tablet carries the same two settings and the same tier contract as the Windows terminal, implemented as a palette table rather than a second design language. `Theme` (VectronTablet/Theme.cs) owns both the persisted choice (`Preferences`: `theme`, `graphics`) and the resolved tier; `Theme.Apply()` runs in the `App` constructor **before any page is parsed**, so every `StaticResource` lookup binds to the active palette. Changing a setting re-applies the palette and rebuilds the shell (`App.ShowMain()` / `ShowLogin()`), which is what re-resolves page-level resource colours.

### Tablet palette

Same role names as the Windows `Skin`, with the tablet's own accent (`#70C1B3`, the long-standing Android brand teal — the Windows accent is `#47D6AE`).

| Role | Dark | Light |
|---|---|---|
| `Bg` | `#0F141A` | `#F3F6FA` |
| `Panel` | `#1A222C` | `#FFFFFF` |
| `Panel2` | `#243040` | `#E8ECF3` |
| `Kpi` (quiet pill / free table) | `#1C2432` | `#EDF1F6` |
| `Mint` (accent) | `#70C1B3` | `#0E7C63` |
| `MintDeep` (pressed) | `#5AA99B` | `#0A6552` |
| `Ink` (text on accent) | `#0F141A` | `#FFFFFF` |
| `Txt` | `#E9EFF5` | `#131821` |
| `TxtDim` | `#8C99A8` | `#586476` |
| `HudLine` (hairline / stroke) | `#2E3A4B` | `#D6DDE7` |
| `ReadyBg` (order ready) | `#16321F` | `#E4F3E8` |
| `TakenBg` (table taken) | `#332A16` | `#FBF0DA` |
| `Price` | `#47D6AE` | `#0E7C63` |
| `Danger` / `DangerInk` | `#E07A5F` / `#F06A5A` | `#B03A2E` |
| `Amber` | `#F4B860` | `#96600A` |
| `Green` | `#4E9F6E` | `#1F6B45` |
| `PlusBg` / `EightySixBg` | translucent mint / warm grey | translucent deep green / warm rose |
| `OnFill` (text on saturated fill) | `#FFFFFF` | `#FFFFFF` |

Light uses the same deep green `#0E7C63` as the Windows light accent, so the two products stay one family. Style setters in `App.xaml` (`PrimaryBtn`, `GhostBtn`, `TableBtn`, `ChipBtn`, `SectionLbl`, `DimLbl`) are re-pointed explicitly by `Theme.SetStyleColor(...)` — styles are parsed once, so replacing the dictionary entries alone never reaches them.

### Tablet shell (P1.9)

- **Tables page** — the Windows app-bar language at tablet scale: vector `VectronMark` (`VectronMarkDrawable`) + `VECTRON` / `W A I T E R` lockup, notification pill, `⋯` Appearance button, `Exit`. Below it the page head keeps the P1.7 mint rail + LED status dot + hairline. Rooms are **pills** (active = `Mint` fill + `Ink` text, inactive = `Kpi` fill + `TxtDim`), tables are rounded 14 cards on `ReadyBg` / `TakenBg` / `Kpi` with a `HudLine` 1px stroke and `Theme.Depth` on High.
- **Order page** — no hardcoded colours left: tiles are `Panel` with a `HudLine` stroke, the `+` badge is `PlusBg` with `OnFill` ink, the `86` badge is `EightySixBg` with `DangerInk` ink, price colour is `Price` / `DangerInk`, basket rows sit on `Panel`.
- **Appearance entry** — `⋯` on the Tables page opens two native `DisplayActionSheet` levels (`Appearance · <Theme> · <resolved reason>` → Theme → Dark/Light, Graphics → Auto/High/Lite). Native sheets on purpose: they follow `UserAppTheme`, and they sidestep the gesture/`IsEnabled` defect backlog (a custom gesture surface would fight the P1.4/P1.5 press handling).
- **System chrome** — `MainActivity.ApplySystemBars()` colours the status and navigation bars with `Bg` and flips the status-bar icons (`SYSTEM_UI_FLAG_LIGHT_STATUS_BAR`) for the light palette. It runs on activity creation **and** from `Theme.Apply()`, because a `UiMode` change does not recreate the activity.
- **Motion** — `Theme.Reveal(view)` is the tablet counterpart of `Skin.Animate`: one 150 ms fade + 12 px rise, fired once per page instance on High, a no-op on Lite (the flat baseline paints the final frame immediately). The 3-second tables poll never re-animates.

---

# Implementation contract — building a new screen

**Read this before writing any UI code.** The previous sections describe *what the product looks like*; this one describes *how to reproduce it* so a new screen cannot drift from the shipped ones. Every rule below is derived from code that is already in the repository — nothing here is aspirational.

## C0. Where the truth lives

| Question | Answer lives in | Never in |
|---|---|---|
| What colour is role X? | `Program.cs` → `internal static class Skin` (search `class Skin`) | a new literal in a screen |
| What does the Android palette say? | `VectronTablet\App.xaml` + `Theme.cs` | a hex in a `.xaml` page |
| What is the screen's job / which controls it holds? | `PLAN.md` §10.2 taxonomy table | memory |
| What is the visual grammar? | this file | screenshots |
| How do I build/ship/verify? | `ARCHITECTURE.md` §7 | guesswork |

Three files are the whole design system: **`Skin`** (Windows), **`App.xaml` + `Theme.cs`** (Android), **`DESIGN.md`** (this contract). If a change needs a colour that is not in `Skin`, the change is wrong — find the closest existing role instead of adding one.

## C1. Intent → role (the only colour decision you make)

Pick the row that describes *what the element is for*, not what it looks like. Both columns must exist for every role, because every screen ships in Dark and Light.

| You need… | Windows `Skin.*` | Android key | Notes |
|---|---|---|---|
| Window / page background | `Background` | `Bg` | never white-on-dark |
| Content surface, card, dialog | `Panel` | `Panel` | pairs with `CardBorder` |
| Quieter surface inside a card (row, chip, input well) | `PanelLight` | `Panel2` | one step lighter (dark) / darker (light) |
| Quiet pill, KPI chip, inactive tab | `Kpi` | `Kpi` | the "not selected" fill |
| Input field | `PanelLight` + `Border` | `Panel2` + `HudLine` | focus = `BorderHover` (Win) / `Mint` stroke (Android) |
| Hairline, divider, table rule | `Border` | `HudLine` | exactly 1 px, never thicker |
| Card edge | `CardBorder` | `HudLine` | cards use `CardBorder`, inputs use `Border` |
| Primary action fill | `Accent` | `Mint` | one per screen region, max two visible |
| Text/icon on the primary action | `OnAccent` | `Ink` | dark ink on mint in Dark, white in Light |
| Primary action pressed | `AccentPressed` | `MintDeep` | only via `FlatStates`, never assigned by hand |
| Secondary / neutral button | `PanelLight` | `Panel2` | text `Text` |
| Destructive action | `DangerStrong` | `Danger` | ink `OnFill` |
| Warning state (low stock, expiry, late order) | `Warning` / `WarningSurface` | `Amber` | surface fill + `Warning` text |
| Success / confirmed / in stock | `Success` / `SuccessSurface` / `SuccessText` | `Green` | toggle ON uses the surface pair |
| Body text | `Text` | `Txt` | |
| Secondary text (dates, subtitles, units) | `TextDim` | `TxtDim` | recess by colour, not size |
| Micro-copy / group heading | `TextFaint` | `TxtDim` | 9 pt uppercase on Windows |
| Disabled text | `TextDisabled` | `TxtDim` (40 % opacity) | never recolour the fill only |
| Price | `Accent` | `Price` | 86'd price = `DangerText` / `DangerInk` |
| Selected nav pill | `NavActive` + `Accent` text | `Mint` + `Ink` text | + 3 px left rail on Windows |
| Hover on a transparent pill | `PillHover` | `Panel2` | |
| Text on a saturated fill | `OnFill` | `OnFill` | always white, both themes |
| Scrollbar / menu chrome | `SkinColorTable` | native | do not hand-roll |

**Contrast gate:** body text ≥ 4.5:1 on its own background in **both** themes. If a pairing fails, the pairing is wrong — not the palette. Light never uses `#47D6AE` as text; it uses `Accent` (`#0E7C63`).

## C2. Geometry scale (Windows)

Copy these numbers. A screen that invents its own spacing is the main source of "it looks like a different app".

| Element | Value |
|---|---|
| App bar height | **72** |
| Page head height | **88** (3 px accent rail at x=0,y=6; title x=14; 1 px `Border` docked bottom) |
| Settings rail width | **236** |
| Window padding (content) | **20** |
| Gap between major regions | **18** |
| Gap between rows | **8** |
| Card content inset | **20** |
| Card radius | **14** |
| Small card / KPI radius | **10** |
| Pill radius | **18** (`height / 2` for a true pill) |
| Nav pill height | **36** |
| Button height (primary/secondary) | **36**; **38** for the one main action on a page |
| Login-style field height | **36** |
| Toggle row height | **38** |
| Table/room card radius | **12** |
| Hairline | **1 px**, always |
| Accent rail | **3 px** wide, 16–34 px long |
| Icon / mark size | **28** in a bar, **36** on a login card |
| Dialog default | 380×460 (small) / 700×600 (manager) / **940×700** (Settings, min 820×560) |

Radii are applied with `Region` on buttons (`Skin.FlatStates` does it for you) and with `Skin.RoundRect` inside `OnPaint` for anything drawn. **Never** set a rounded `Region` on a control that also paints its own outline — the region clips the stroke at the corners (this is why inactive category chips are square-segment, see P1.7).

## C3. Typography scale (Windows, Segoe UI)

| Use | Font |
|---|---|
| Brand / page title | `Segoe UI Semibold` **17 pt** (16 pt in dialogs) |
| Section heading inside a card | `Segoe UI Semibold` **10 pt** |
| Body / button / label | `Segoe UI` **10 pt** |
| Secondary line | `Segoe UI` **9 pt**, `TextDim` |
| Micro-copy / rail group heading | `Segoe UI Semibold` **9 pt**, uppercase, `TextFaint` |
| KPI amount | `Segoe UI Semibold` **16–24 pt** (scale with the chip, cap at 24) |
| Total | `Segoe UI Semibold` **26 pt** |
| Never | < 8.5 pt, letter-spacing, `ToUpper()` on a button label |

Button labels are never case-changed — fixed-width buttons must not clip FR/AR strings.

## C4. Component recipes (copy these calls)

### Card / surface
```csharp
var card = new SkinPanel {
    Radius = 14, Edge = Skin.CardBorder, Depth = true,
    BackColor = Skin.Panel, Padding = new Padding(20)
};
// add to a parent, then give it a real Size/Location (or Dock) — SkinPanel does not auto-size
```
`Gradient` defaults to `true` and is honoured **only** when `Skin.IsHigh`; `Depth` likewise. Lite therefore paints the identical solid fill as a plain `Panel`.

### Pill button (primary / secondary / danger)
```csharp
var b = new Button {
    Text = Tr.T("Save"), Size = new Size(120, 36),
    FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
    BackColor = Skin.Accent, ForeColor = Skin.OnAccent
};
b.FlatAppearance.BorderSize = 0;
Skin.FlatStates(b);   // radius + hover/pressed/disabled steps, base stored in b.Tag
```
- Change the base colour later with **`Skin.SetFlatBase(b, colour)`** — never `b.BackColor = …`, or hover/press keep stepping from the old base.
- Primary = `Accent`/`OnAccent`; secondary = `PanelLight`/`Text`; danger = `DangerStrong`/`OnFill`; warning = `WarningDeep`/`OnFill`; success = `Success`/`OnFill`; neutral info = `Info`/`OnFill`.
- The **one** primary action of a page is the only `Accent`-filled button in that region.

### Toggle (feature flag) — the P7.3 recipe
```csharp
bool on = value;
var b = new Button { Text = (on ? "● " : "○ ") + label, Size = new Size(width, 38),
                     FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft,
                     Padding = new Padding(12, 0, 0, 0) };
b.FlatAppearance.BorderSize = 0;
Skin.SetFlatBase(b, on ? Skin.SuccessSurface : Skin.PanelLight);
b.ForeColor = on ? Skin.SuccessText : Skin.TextDim;
b.AccessibleName = label; b.AccessibleDescription = on ? "on" : "off";
Skin.FlatStates(b);
```
Flipping it must call `LayoutRows()` so the dependent parameter button appears **in the same frame** (see `PLAN.md` P7.3). A toggle with no screen behind it is a bug — pair it with a `Configure` button or a `FeatureCard`.

### Nav pill / category pill / room chip
```csharp
var p = new Button { Text = label, Size = new Size(w, 36), FlatStyle = FlatStyle.Flat,
                     BackColor = active ? Skin.NavActive : Skin.Kpi,
                     ForeColor = active ? Skin.Accent : Skin.TextDim };
p.FlatAppearance.BorderSize = 0;
Skin.FlatStates(p);
```
Active pills on Windows also paint a 3 px `Accent` rail on their left edge and pad text past it (`Padding(10,3,3,3)`).

### Page head (inside a page, under the app bar)
3 px `Accent` rail at `(0, 6, 3, 34)` · title at `x = 14`, 17 pt Semibold, `Text` · subtitle/date at `x = 16`, 9 pt, `TextDim` · 1 px `Border` docked bottom.

### App bar
`SkinPanel { Dock = Top, Height = 72, BackColor = Skin.AppBar, Gradient = false }` + a 1 px `AppBarBorder` hairline at its bottom. Left: `VectronMark` (28 px) + two-line lockup. Right: status LED, KPI chip, operator chip, `⋯` overflow — in that order, right-aligned with 12 px gaps.

### Dialog
`BackColor = Skin.Background`, `ForeColor = Skin.Text`, `Font = Segoe UI 10 pt`, `StartPosition = CenterParent`. A single `SkinPanel` card (`Radius = 16`, `Edge = Skin.CardBorder`, `Depth = true`) holds the content. `Shown += (_, _) => Skin.SlideWindow(this);` — a no-op in Lite. Every dialog has a `← Back` (or `Cancel`) in the same top-left slot and honours `Esc`.

### List / table rows
Owner-drawn (`ListView.OwnerDraw` or a `Panel` per row) using: row fill `Panel`, alternating `Kpi` if needed, selected `NavActive` + `Accent` text, header `TextFaint` 9 pt uppercase, 1 px `Border` between rows, row height 32–38. Never rely on the system selection colour.

### Empty state
Two labels, never one: a Semibold `Text` title ("No products selected yet") + a `TextDim` hint ("Choose products from the menu."). Both translated. Same in lists, baskets and consoles.

### KPI chip
`SkinPanel { Radius = 10, BackColor = Skin.Kpi }`, caption 9 pt `TextFaint` uppercase, amount Semibold `Accent`, gradient + `CardDepth` on High.

### HUD decoration (optional, identity motif)
`Hud.Brackets(g, rect, Skin.Accent, arm: 16, inset: 5)` inside `OnPaint` for signature surfaces (login card, totals panel), and `HudPanel { Edge = Skin.CardBorder }` when a panel should paint its own 1 px edge + brackets. Static, painted once per state change — no timer.

## C5. Motion (High only, one timer)

```csharp
Skin.Animate(150, t => { /* t = 0..1 */ }, done: null);   // no-op in Lite
Skin.SlideWindow(form);                                    // 22 px rise, no-op in Lite
mark.Progress = t;                                         // VectronMark draw-in 0..1
```
Rules: **one** shared 150 ms timer (`Skin.Animate`), never a per-frame loop, never an idle timer, never `Opacity` tweening of a whole form. `Lite` must paint the final frame on the first paint. Android: `Theme.Reveal(view)` / `Theme.Depth(border)` — both already early-return on `!IsHigh`.

## C6. Blueprints for the screens P7/P8 will add

These are the shapes the new work must take. They reuse everything above; they introduce no new colour and no new metric.

### Settings hub (`SettingsForm`, P7.1)
```
┌ app bar 72 ────────────────────────────────────────────────────────────────┐
│ ◉ VECTRON        breadcrumb: Settings › Inventory        [← Back] [Done]   │
├ rail 236 ────────────┬ page head 88 ────────────────────────────────────────┤
│ SETUP                │ ▎ Ingredients                    (subtitle, dim)     │
│  Appearance          │ ───────────────────────────────────────────────────  │
│  Catalog             │ ┌ card 14 ─────────────────────────────────────────┐ │
│  Devices & Network   │ │ SECTION (9 pt uppercase TextFaint)                │ │
│  System              │ │ ○ Auto-86 when an ingredient hits its minimum    │ │
│ SERVICE              │ │ [ Configure ]            ← appears immediately    │ │
│  Service & Tables    │ └──────────────────────────────────────────────────┘ │
│  Kitchen             │ ┌ card 14 ─────────────────────────────────────────┐ │
│  Payments            │ │ … 8 px between rows, 18 px between cards          │ │
│  Customers           │ └──────────────────────────────────────────────────┘ │
│ TEAM & STOCK         │                                                      │
│  Staff               │   content AutoScroll, padding 20                     │
│  Inventory           │                                                      │
│ INSIGHT              │                                                      │
│  Reports             │                                                      │
│  Receipts & Display  │                                                      │
└──────────────────────┴──────────────────────────────────────────────────────┘
```
Rail: group headings 9 pt uppercase `TextFaint`, 12 pill rows (h 38, radius 18), active = `NavActive` + `Accent` text + 3 px left rail, hover = `SidebarItemHover`, `AutoScroll`. Content: `SkinPanel` + `AutoScroll` + `Padding(20)`. `← Back` and `Done` live in the app bar, never in the content. The old `ShowMainOverview` page is deleted — its buttons move into the 12 pages per `PLAN.md` §10.2.

### Settings page (any of the 12)
`Section("Behaviour")` cards first (the toggles), then `Section("Configure")` cards (the parameter buttons). A parameter button is **never** above its toggle. Toggling reflows with `LayoutRows()` — no page rebuild, no scroll jump, focus stays on the toggle.

### Stock console (`StockManagerForm`, P8.2)
```
app bar (mark, "Stock", breadcrumb, ← Back, Done)
head: ▎ Stock        Overview · Ingredients · Products · Recipes · Movements …
pill row (radius 18) = the tabs; active Mint/NavActive
┌ KPI 10 ┐ ┌ KPI 10 ┐ ┌ KPI 10 ┐ ┌ KPI 10 ┐
│ 3 low  │ │ 1 240  │ │ 12 kg  │ │ 2 x 86 │   ← caption 9 pt, amount Semibold Accent
└────────┘ └────────┘ └────────┘ └────────┘
┌ card 14 ────────────────────────────────────────────────────────────────┐
│ Name            Unit  On hand  Min   Cost   Value   Supplier   Expiry   │  ← header TextFaint
│ Flour           g       4 800  1 000 0.002   9.60   Metro      —        │
│ Tomato          g       2 900    500 0.003   8.70   Metro      —        │  ← row Panel, 1 px Border
└─────────────────────────────────────────────────────────────────────────┘
[ + Add ingredient ]  [ Adjust ]  [ Import CSV ]  [ Export CSV ]
```
A tab whose feature flag is OFF shows the `FeatureCard` off state ("Turn on to configure" + an inline `Turn on` pill), never an empty grid. Low rows tint with `WarningSurface`; out-of-stock rows use `Danger` text.

### Recipe editor (`RecipeEditorForm`, P8.2.3) — the pizza screen
```
app bar ("Pizza", breadcrumb Inventory › Recipes, ← Back, Done)
┌ left 260 (search + product list) ┐ ┌ right: editor card 14 ──────────────────────┐
│ search [__________]              │ │ Pizza · 12.00 EUR        [KPI] cost 1.25    │
│ ▸ Pizza        ✓ recipe          │ │ food cost 10 % · margin 90 %  (Success/Warn)│
│   Coca-Cola    no recipe         │ │ Portions: [ 1 ]  (batch → divide on save)   │
│   Espresso     no recipe         │ │ ┌ Ingredient ┬ Qty ┬ Unit ┬ Cost ┬ ✕ ┐     │
│                                  │ │ │ Flour      │ 100 │ g    │ 0.20 │ ✕ │     │
│                                  │ │ │ Tomato     │  50 │ g    │ 0.15 │ ✕ │     │
│                                  │ │ │ Cheese     │ 100 │ g    │ 0.90 │ ✕ │     │
│                                  │ │ └────────────┴─────┴──────┴──────┴───┘     │
│                                  │ │ [ + Add ingredient ]      [ Clear recipe ] │
└──────────────────────────────────┴─┴────────────────────────────────────────────┘
```
The unit combo is limited to the ingredient's own family (`g↔kg`, `ml↔L`, `pcs`); every saved quantity is canonicalised to the ingredient's unit. A product **with** a recipe shows its `Products` stock row as "not used — recipe takes over" (greyed) so nothing is deducted twice.

## C7. Anti-drift self-check (mandatory before calling a screen done)

1. `Select-String -Path Program.cs -Pattern 'Color\.FromArgb'` → every hit is inside `Skin` (or one of the 12 known legitimate ones). **Zero** new hits in a screen.
2. `Select-String -Path "VectronTablet\**\*.xaml" -Pattern '#[0-9A-Fa-f]{6}'` → no hits outside `App.xaml`.
3. No `new Font("Segoe UI", …)` outside the scale in C3; no size below 8.5 pt.
4. No magic number from C2 replaced by a local constant.
5. Every `BackColor` set on a `Skin.FlatStates` button is done through `Skin.SetFlatBase`.
6. Every effect (`Gradient`, `Depth`, `Animate`, `SlideWindow`, `Reveal`) is behind `Skin.IsHigh` / `Theme.IsHigh` — by construction these helpers already are; a raw `Timer`/`Invalidate` in a paint path is a failure.
7. Both TFMs build: `powershell -NoProfile -File ".opencode\tools\build-win.ps1" -Tfm all` → **0 errors**.
8. Rendered in **all four states** — Dark×High, Dark×Lite, Light×High, Light×Lite — and captured to `.opencode\screenshots\`.
9. Every new string exists in `Lang.cs` for `en`, `fr` **and** `ar`.
10. `break-ui` pass: 80-char name, unbreakable string, 1-char name, Arabic, CJK, emoji, `0.00`, `999999.99`, empty string → no clipped label, no crash.

If any check fails, the screen is not done — the check is cheaper than the rework.

## C8. Design "never" list

- Never hardcode a colour, a font family or a radius outside C2/C3.
- Never introduce a second accent colour. Status colours (`Warning`, `Danger`, `Success`, `Info`) are for state only, never for decoration or a primary action.
- Never make Light an inversion or a filter — it is its own palette.
- Never run a High effect in Lite.
- Never use emoji as an icon; use the vector mark, a glyph drawn in `OnPaint`, or a text token (`●`, `○`, `✕`, `⋯`).
- Never use a modal `MessageBox` for a routine in-service notice — use the app-bar banner (`PLAN.md` P8.4).
- Never put a toggle on screen without the screen it configures.
- Never change the shipped defaults: **Theme = Dark, Graphics = Auto**.

