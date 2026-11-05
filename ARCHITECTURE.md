# Vectron POS — Architecture & Handover

> **Read this before touching code.** It describes how the two apps are built, where every
> subsystem lives, what the appearance contract is, how to build/ship/verify, and what is
> still open. `PLAN.md` is the session log and the phased plan; **this file is the map**.
> Together they let a different machine (and a different AI agent) continue the work.

---

## 1. The product

Two apps, one design language, sold by Vectron (client: Boitexinfo).

| App | Tech | Source | Ships as |
|---|---|---|---|
| **Vectron POS** (the till / server) | C# WinForms, dual TFM `net10.0-windows` + `net48` | `Program.cs` (single file, ~13.5k lines) + 9 helper files | 3 zips (Win10-x64, Win7, Win7-Full) |
| **Vectron Waiter** (the tablet) | .NET MAUI `net10.0-android` | `VectronTablet\` | `Vectron-Waiter-v1.2.0.apk` |

The POS is the **server**: it owns the data and serves a REST API on port 8080. The tablet is a
thin client that talks to that API over the LAN. **Every UI/logic change must reach all four
variants** (Win10, Win7, tester build, Android).

---

## 2. Repository map

```
Program.cs                 Windows app: everything (store, shell, screens, printing, REST, Skin)
Lang.cs                    translations (en / fr / ar) — Tr.T("...") keys
License.cs / LicenseForm.cs  offline ECDSA licence gate + activation dialog
TicketLayout.cs            ticket designer (TicketDesignerForm) + receipt layout model
Code39.cs / QrCode.cs      barcode + QR renderers (no external packages)
TicketPdf.cs               (inside Program.cs) minimal PDF writer for email/receipts
Net48Polyfills.cs          C# 9/10 helpers for the net48 target
PosCompat.cs               small TFM shims
MANUAL.md                  user manual (French)
DESIGN.md                  design language: palettes, tiers, shell spec (validated)
PLAN.md                    phased plan + full session history (auto-loaded via opencode.json)
ARCHITECTURE.md            this file

VectronTablet/
  App.xaml                 token dictionary (the ONLY place colours are declared) + shared styles
  App.xaml.cs              CreateWindow: Shell when signed in, else LoginPage
  AppShell.xaml            one ShellContent -> TablesPage
  Theme.cs                 appearance layer: palettes, Apply(), SetTheme/SetQuality, Reveal/Depth/Press
  Hud.cs                   HUD corner-bracket drawable (P1.7 layer)
  MauiProgram.cs           DI + the Android ButtonHandler mapper that kills MaterialButton tint
  Models/Dtos.cs           API DTOs
  Pages/LoginPage, TablesPage, OrderPage, AlertsPage, ModifierSheetPage
  Services/PosApi.cs       every REST call the tablet makes
  Services/AppState.cs     session, cart, feature flags, FlushQueue
  Services/OfflineQueue.cs queued order posts while offline
  Services/Alert*          kitchen-alert polling + notification + battery exemption
  Platforms/Android/MainActivity.cs   ApplySystemBars() (status/nav bar colour + light icons)

.opencode/tools/           build-win.ps1, build-apk.ps1, emu.ps1  (toolchain-resolving helpers)
.opencode/screenshots/     verification captures (p13…p4 sets)
.opencode/skills/          design skills (ui-ux-pro-max, emil-design-eng, break-ui, …)
```

---

## 3. Windows app — how `Program.cs` is organised

One file, but strictly layered top-to-bottom. Anchor line numbers shift as code is added —
search for the type name instead.

### 3.1 Data + settings

| Type | Role |
|---|---|
| `FeatureSettings` | every feature flag + `Language`, `ApiEnabled/ApiKey`, `Theme`, `GraphicsQuality`, `ResolvedQuality`, `ResolvedReason`. Persisted to `client database\feature-settings.json`; **missing keys fall back to defaults** (Dark + Auto), so old files keep working. |
| `PosStore` | the whole data layer. `dataFolder` defaults to `<exe>\client database`, can be switched to a network path (`RegisterSettings.NetworkPath`). One JSON file per collection: `pos-data.json` (sales), `open-orders.json`, `products.json`, `families.json`, `rooms.json`, `operators.json`, `customers.json`, `modifiers.json`, `ingredients.json`, `printer-destinations.json`, `register-settings.json`, `feature-settings.json`, `shifts.json`, `daily-counters.json`, `product-variants.json`. |

Products are plain records: `Name, Category, Price, Color, Family, ImagePath, PrinterDestination,
Barcode, Weighted`. **Categories are a fixed list** — the tab strip is hardcoded to
`Popular / Mains / Drinks / Desserts` (`BuildUi`), so a product whose `Category` is anything else
never shows on the till.

### 3.2 Appearance — `Skin` (the token layer)

`internal static class Skin` — every colour in the Windows app comes from here. ~60 roles, each a
`static Color` property that reads `IsLight` and returns the Dark or Light value:

```
Background Panel PanelLight PanelHover PanelPressed InputDeep HeaderPanel Sidebar*
Border BorderHover BorderPressed
Text TextDim TextMuted TextSoft TextDisabled TextFaded TextFaint NavText
OnFill OnAccent Accent AccentPressed AccentBright
Success* Info InfoAlt Danger* Warning* Neutral Purple Pink Teal
NavActive* AppBar AppBarBorder PillHover Kpi CardBorder
ModifierText NoteText ChangeText Shadow Highlight
```

Supporting API:

| Member | Meaning |
|---|---|
| `Skin.Apply(FeatureSettings)` | called **first** in the `PosForm` ctor: sets `Mode`, resolves `Quality` (Auto → High/Lite), stores the reason. |
| `Skin.Mode` / `Skin.Quality` / `Skin.IsHigh` / `Skin.IsLight` | current state |
| `Skin.Detect()` / `Skin.Resolve()` | Auto: High only if RAM ≥ 4 GB **and** ≥ 2 cores **and** not RDP **and** not Windows 7 |
| `Skin.Preview(theme, quality)` | `IDisposable` scope (`SkinScope`) used by the Settings live preview |
| `Skin.Shade`, `Skin.MixToward`, `Skin.Step`, `Skin.RoundRect`, `Skin.FillSurface`, `Skin.CardDepth` | colour/geometry helpers |
| `Skin.FlatStates(button)` / `Skin.SetFlatBase(button, colour)` | the **one** flat button implementation: base fill kept in `button.Tag`, hover/press = `Step` (lighter on dark, darker on light), disabled = 65 % toward `Background` + `TextDisabled`, radius `max(6, min(w,h)/24)`. `PosForm.ApplyFlatStates`/`SetFlatBase` just delegate here. |
| `Skin.Animate(ms, step, done)` | **the only** animation timer: one shared 150 ms `System.Windows.Forms.Timer`, cancel-safe, **no-op on Lite**. |
| `Skin.SlideWindow(form)` | High-only dialog entrance |

**Rule: new code must never write `Color.FromArgb(...)`.** The 12 remaining literals are inside
`Skin` itself (alpha overlays), `Shade`/`MixToward`, the white QR label card and the white
receipt-paper preview.

### 3.3 Shell (P1.8) — `PosForm`

`PosForm` is the main window. Structure:

```
appBar (72 px, Dock=Top, Skin.AppBar)
  VectronMark (vector logo: mint ring + chevron + HUD brackets, draws itself in on High)
  VECTRON / "P O S" wordmark
  nav pills: Order · Rooms · Operator · Settings   (StylePill / SetActiveNav / NavItem)
  status LED + "MAIN PC" micro-copy (tooltip = IP + print server)
  sales KPI chip (caption + amount, gradient + depth on High)
  operator chip
  "⋯" overflow pill -> ShowOverflow() -> ContextMenuStrip rendered by SkinColorTable
       (9 secondary screens: Clock, Stock, Kitchen, 86 board, Bookings, Schedule,
        Gift cards, Purchases, Cash drawer — shown only when the feature is enabled)
pageHead (88 px: 3 px mint rail, greeting, date, 1 px hairline)
workspace: product grid (left) + floating order card (SkinPanel, radius 14, depth on High)
```

The old 205 px left sidebar is **gone** (PLAN §9 P1.8). Nav items are `Available`-gated, not
`Visible`-gated — `ToolStripMenuItem.Visible` reads `false` before the drop-down is first shown.

Entrance motion (High only): logo draw-in 420 ms + order-card slide 220 ms on `Shown`. Lite paints
the final frame immediately.

### 3.4 Screens

All are `Form` subclasses in the same file: `LoginForm` (centered card + mint focus rails),
`OperatorPickerForm`, `OperatorManagerForm`, `SettingsForm` (categories incl. **Appearance**),
`ProductManagerForm`, `FamilyManagerForm`, `RoomManagerForm`, `PrinterManagerForm`,
`TicketStyleEditorForm`, `TicketDesignerForm` (in `TicketLayout.cs`), `SalesReportForm`,
`CurrencySettingsForm`, `KitchenDisplayForm`, `KioskForm`, `CustomerDisplayForm`,
`NetworkSettingsForm`, `KeyboardForm` (on-screen keyboard, `KeyboardMessageFilter`).

`ProductTile` (a `Panel`): flat 3-state (normal/hover/pressed from `Skin`), shared
`Radius = max(6, Width/24)` for the region + image clip, 1 px border painted in `OnPaint` after
content, cover-fit image (full width × 80 % height), one text row: name **ellipsized** to the
available width + right-aligned price, `86` badge + strikethrough when out of stock.

### 3.5 Printing

`RawPrinter` (Win32 spooler), `TcpPrintServer` / `TcpPrintClient` (port 9100 style),
`TicketLayout` model + `TicketPdf`, `CatalogImageStore` (product images).

### 3.6 REST server — `QrOrderServer`

Serves on `0.0.0.0:8080` when `Features.ApiEnabled`. **Auth = `?key=<ApiKey>` query parameter**
(not a header). Endpoints:

| Route | Method | Purpose |
|---|---|---|
| `/api/tablet/bootstrap?key=` | GET | POS identity + feature flags + currency + operators list |
| `/api/tablet/login` | POST | operator PIN sign-in |
| `/api/tablet/menu` | GET | categories + products (prices, colours, image names) |
| `/api/tablet/image?name=` | GET | product image bytes |
| `/api/tablet/map` | GET | rooms + tables + live totals |
| `/api/tablet/order?room=&table=` | GET | open order for a table |
| `/api/tablet/order` | POST | add lines |
| `/api/tablet/fire` | POST | send to kitchen |
| `/api/tablet/bill` | POST | print bill / close |
| `/api/tablet/line-edit` | POST | qty / void / note edit |
| `/api/tablet/alerts` · `/alerts/dismiss` | GET/POST | kitchen call alerts |
| `/api/menu`, `/api/products`, `/api/sales`, `/api/orders`, `/api/order`, `/api/call`, `/api/status` | mixed | legacy/QR endpoints |

### 3.7 Licensing

`License.cs` + `LicenseForm.cs` + `LicenseGen\`. `Program.Main` loops `while (!License.IsActive)` —
**never bypass**. The fingerprint is per-TFM, so one PC legitimately needs two keys (net10 and
net48 differ). `license.dat` + `license.time` live in `client database\` and are **never** shipped.

---

## 4. Android app — how `VectronTablet` is organised

### 4.1 Tokens

`App.xaml` declares every colour once (`Bg Panel Panel2 Kpi Mint MintDeep Ink Amber Danger Green
Txt TxtDim HudLine ReadyBg TakenBg PlusBg EightySixBg DangerInk Price OnFill`) plus the shared
styles (`PrimaryBtn GhostBtn TableBtn ChipBtn SectionLbl DimLbl`). **No page may declare a raw
hex.** The MAUI template's `Resources/Styles/Colors.xaml` + `Styles.xaml` were deleted (P2) — they
were the last source of template purple.

`Theme.cs` is the runtime layer and mirrors the Windows `Skin`:

| Member | Meaning |
|---|---|
| `Theme.Apply()` | reads `Preferences` (`theme`, `graphics`), resolves the tier, **writes every palette key into `Application.Resources`**, sets `Application.Current.UserAppTheme`, recolours the system bars, then re-points every themed style setter. Called from the `App` ctor **before** any page is parsed. |
| `Theme.SetTheme/SetQuality` | persist → `Apply()` → `Rebuild()` |
| `Theme.Rebuild()` | `Restyle()` the live tree, then `App.ShowMain()/ShowLogin()` |
| `Theme.Restyle()` | walks the visual tree and rewrites colours that XAML resolved *before* the switch (see gotcha 7.2) |
| `Theme.C(key)` / `Theme.S(key)` | read a token / a style from the dictionary |
| `Theme.Reveal(view)` / `Theme.Depth(border)` | 150 ms fade+rise / soft shadow — **both no-op on Lite** |
| `Theme.Press(c)` / `Theme.PressBind(b, onClick, canClick)` | flat pressed feedback |
| `Theme.IsHigh` / `Theme.IsLight` | tier / palette |

`Platforms/Android/MainActivity.ApplySystemBars()` colours the status + navigation bars with `Bg`
and flips `SYSTEM_UI_FLAG_LIGHT_STATUS_BAR` for Light. It runs on activity creation **and** from
`Theme.Apply()` (a `UiMode` change does not recreate the activity).

### 4.2 Screens

`LoginPage` (address + API key + PIN, connect → operator picker → sign in), `TablesPage`
(new P1.9 shell: `VectronMarkDrawable` + VECTRON/WAITER lockup, notification pill, `⋯` Appearance,
Exit, page head with mint rail + LED, room **pills**, table cards radius 14 with `Theme.Depth` on
High), `OrderPage` (product grid, basket rows, qty stepper, Send to kitchen), `AlertsPage`,
`ModifierSheetPage`.

**Appearance UI on the tablet:** `⋯` → native `DisplayActionSheet` (`Appearance · <theme> ·
<resolved tier> — <reason>` → `Theme:` / `Graphics:` → second sheet Dark/Light or
Auto/High/Lite). Choices persist in `Preferences`; **defaults stay Dark + Auto**.

### 4.3 Networking

`PosApi` builds every request from `Preferences` (`pos_url`, `api_key`) and appends `?key=`.
**The emulator must be pointed at the host's LAN IP** (e.g. `http://192.168.1.80:8080`) — never
`localhost`, never the guest NAT address (`172.16.1.x`). `OfflineQueue` retries queued posts.

---

## 5. The appearance contract (both apps)

| | **Lite** (default on weak/Win7/RDP) | **High** |
|---|---|---|
| Fills | solid colours only | solid + 2-stop vertical gradients (app bar, primary buttons, KPI chip, cards) |
| Depth | 1 px borders, flat hover/press steps | layered soft shadow under cards/dialogs (`Shadow`, no blur filter) |
| Motion | none — instant state change | 150 ms fades/slides via one shared timer, cancel-safe |
| Cost | = the pre-P1.8 baseline | one extra paint pass per state change; idle CPU unchanged |

Non-negotiable in **both**: no per-frame animation loops, no idle timers, no whole-form `Opacity`
tweening, no `ColorMatrix`/`PathGradient` glows, no new NuGet packages for UI, system fonts only,
everything compiles for **net10 and net48**, Lite runs on Win7 SP1 + .NET 4.8.

Auto-detect (default) resolves to High only when RAM ≥ 4 GB **and** ≥ 2 cores **and** not Remote
Desktop **and** not Windows 7. The *choice* and the *resolved tier + reason* are stored separately
so a hardware change re-evaluates on the next launch.

Defaults after install, in both apps: **Theme = Dark, Graphics = Auto**.

---

## 6. Build, run, ship

### 6.1 Toolchain

| | DEV PC (`serveur-it`, current) | HOME PC (`fullm`) |
|---|---|---|
| Project | `F:\vscode projects\VectronPOS` | `D:\VectronPOS` |
| .NET SDK | 10.0.403 at `C:\Users\serveur-it\.dotnet` | `C:\Program Files\dotnet` |
| JDK 17 | `C:\Java\jdk-17.0.20.1+1` | `C:\Program Files\Microsoft\jdk-17.0.21.21-hotspot` |
| Android SDK | `C:\Android\Sdk` | `C:\Android\Sdk` |
| Emulator | LDPlayer 9 at `C:\LDPlayer\LDPlayer9`, `adb -s emulator-5554` | LDPlayer 14 (`F:\LDPlayer\LDPlayer14`) |

`C:\Program Files\dotnet` on the DEV PC is a **broken stub** ahead of the machine PATH — use
PowerShell (the profile patches PATH) or the helper scripts, never plain `dotnet` from `cmd.exe`.

### 6.2 Build

```powershell
# Windows (both TFMs), output -> %LOCALAPPDATA%\Temp\opencode\build\win10 | win7
powershell -NoProfile -File ".opencode\tools\build-win.ps1" -Tfm all

# Android -> repo root Vectron-Waiter-v1.2.0.apk
powershell -NoProfile -File ".opencode\tools\build-apk.ps1"

# Emulator control
powershell -NoProfile -File ".opencode\tools\emu.ps1" launch|shot|tap|swipe|text|key|install|start|ui|log
```

### 6.3 Ship (P5 = the ship-everything step)

1. `dotnet publish` net10 + net48 (or `build-win.ps1`).
2. Copy **binaries only** into `publish-win10-x64\` and `publish-win7\` — **never** touch their
   `client database\`.
3. Rebuild the three zips + the APK; zips must contain `images\` (7 PNGs) + a starter
   `client database\products.json`, and must **never** contain `license.dat`, `license.time`,
   `feature-settings.json` or `.pdb`.
4. Restart the POS from the updated folder; confirm `/api/tablet/bootstrap?key=…` returns 200.
5. `tester-release` was deleted by the user — do not recreate it unless asked.

---

## 7. Verification workflow (and the traps)

### 7.1 Windows

* `PrintWindow(hwnd, hdc, 2)` captures the window; `FindWindow` is unreliable from PowerShell.
* **UI Automation is the ground truth** for layout: `uiadump.ps1 -ProcId <pid>` prints every
  element with its rect. Modal dialogs appear as `[Window]` descendants.
* Pixel probes (`px2.ps1 -Png … -Points "x,y;…"`) and histograms (`region.ps1 -Rect … -Top n`)
  verify palette/tier without seeing the image.
* State injection: `runstate.ps1 -Theme dark|light -Quality auto|high|lite -Tag <tag>` rewrites the
  **sandbox** `feature-settings.json`, restarts the sandbox exe and captures
  `.opencode/screenshots/p18-main-<tag>.png`.
* Expected signals: Dark×High app bar `#0F141C`, card gradient `#1F2531`; Dark×Lite flat `#1B222E`;
  Light×High bar `#FFFFFF`, bg `#F3F6FA`, card `#F3F3F3`; Light×Lite card `#FFFFFF`.

### 7.2 Android

* `ashot.ps1 -Out x.png` = `adb shell screencap -p /sdcard/x.png` + `adb pull`.
  **Never use `adb exec-out screencap -p > file.png`** — PowerShell's `>` re-encodes stdout as
  UTF-16 and silently produces an invalid PNG (looks sized, fails to decode). `emu.ps1 shot` was
  fixed to the pull method.
* `uiautomator dump` **does** see the native action sheets (it only fails transiently mid-animation)
  — dump first, then tap the exact `bounds` centre. Dialog rows are not where you'd guess:
  main sheet rows ≈ `Theme 303-375`, `Graphics 375-447`, `Close 859..955 × 450..522`;
  sub-sheet rows `Auto 267-339`, `High 339-411`, `Lite 411-483`, `Cancel 855..955 × 486..558`.
  A tap at x = 640 on the `Close` band does nothing (the button only spans x 855–955).
* Expected signals (1280×720): Dark×High status bar `#0F141A` + cards `#1C2432` + taken `#332A16`,
  bg pixel count ≈ 631 k; Dark×Lite ≈ 689 k (the difference **is** the dropped elevation);
  Light bg `#F3F6FA`, cards `#EDF1F6`, taken `#FBF0DA`, accent `#0E7C63`.
* `adb logcat -d | Select-String 'FATAL|Exception'` after every interaction run.

### 7.3 Break-UI pass

Inject worst-case data into the **sandbox** catalog only (`p18-test\client database\products.json`,
back it up first): 80-character names, unbreakable strings, 1-character names, Arabic, CJK, emoji,
`0.00` and `999999.99` prices, an empty name. Windows `ProductTile` ellipsizes by design; verify no
crash + tiles still render in both themes. Restore the backup afterwards.

---

## 8. Known defects / watch list

1. **MAUI Android `VisualStateManager` `Pressed` is a no-op** — pressed feedback must use
   `PointerGestureRecognizer` (`Theme.PressBind`).
2. **Any gesture recognizer on a MAUI `Button` swallows the native click** — a `PressBind`ed button
   must pass its work through `onClick` and declare **no** `Clicked` handler.
3. **`IsEnabled=false` triggers MaterialButton's grey `#C8C8C8` tint** — `MauiProgram` nulls the
   platform `BackgroundTintList`; disabled states are painted, not disabled.
4. **Product tiles fire on press** — a swipe across the grid can add an item (gesture conflict,
   still open).
5. **Basket rows can sit under the fixed bottom bar** on a busy order page (scrollable, but the
   grid starts below the fold).
6. **Draft basket is dropped on back-navigation** (page lifecycle).
7. **XAML `StaticResource` resolves once** — a live theme switch leaves already-parsed elements on
   the old palette. Fixed by `Theme.Restyle()` (a tree walk) + `Rebuild()`; keep it in mind for any
   new page or style.
8. Windows: `ToolStripMenuItem.Visible` reads `false` before its drop-down is first shown — gate
   overflow items with `Available`.
9. Windows: `C:\Program Files\dotnet` is a broken stub on the DEV PC (needs one elevated repair;
   not done without the user).

---

## 9. Where the work stands (as of 4 Nov 2026)

* **P1.1 – P1.9 done** (Windows screen refresh, Flat-HUD layer, `Skin`, Dark/Light + Auto/High/Lite
  on both apps, brand-new Windows shell, Android shell).
* **P2 parity audit done** — every Windows dialog on `Skin.*`, `Color.FromArgb` count outside the
  skin = 0, no raw hex in Android XAML outside `App.xaml`/`Theme.cs`.
* **P3 Lite-safety grep done** — every `Timer`/`Shadow`/`Gradient`/`Invalidate` sits in a
  `Skin.IsHigh` branch, in `Skin.Animate` (no-op on Lite), or is a pre-existing functional timer.
* **P4 verification done** — 4-state matrix captured for both apps:
  Windows `p18-main-{dh,dl,lh,ll}.png`; Android `p4-tables-{dh,dl,lh}.png` + `p4-lh.png` (ll);
  break-UI set `p18-main-break-{dh,lh}.png`.
* **P5 ship done — all four variants.**
  * Windows: `build-win.ps1 -Tfm all` 0 errors → binaries copied **in place** into
    `publish-win10-x64\` (exe 117,428,994 B, SHA256 `EAEF42EB…`) and `publish-win7\`
    (exe 1,001,472 B, SHA256 `C0EA056C…`); `client database\` untouched in both. `F:\win10\`
    exe+pdb refreshed with the same hash.
  * Zips: `VectronPOS-v1.2.0-Win10-x64.zip` (9 entries, 47.3 MB), `…-Win7-x86-x64.zip`
    (19 entries, 3.9 MB), `…-Win7-Full-x86-x64.zip` (21 entries, 119.5 MB). Each has `images/`
    (7 PNGs) + starter `client database/products.json`, 0 forbidden files, files at the **root**
    with `/` separators. Exe hashes inside the zips match the publish folders.
  * APK: `Vectron-Waiter-v1.2.0.apk`, 29,550,110 B, installed on LDPlayer, all four states driven.
  * Live POS restarted from `publish-win10-x64\` (PID 12220) → `/api/menu` 200 on 127.0.0.1 and
    192.168.1.80, `/api/tablet/bootstrap?key=boitexinfo2026` 200 → tablet repopulated its tables.
  * Tablet left on the shipped defaults: **Dark + Auto** (Auto → High, "5.8 GB RAM, 4 cores").
* **P6 report delivered.**
* **Zip-building gotcha (cost real debugging time):** build zips entry-by-entry with
  `Add-Type -AssemblyName System.IO.Compression` + `ZipArchive.CreateEntry(relativePath)` and
  normalise `\` → `/`. `ZipFile.CreateFromDirectory(..., $true)` adds a `win10\` base folder that no
  previous release had, and `[System.IO.Compression.ZipArchiveMode]` does **not** exist unless
  `System.IO.Compression` is loaded (the failure mode is empty 0-byte zips that then report
  "Central Directory corrupt").
* **Temp sandboxes deleted** (`%LOCALAPPDATA%\Temp\opencode\p18-test`, `ship`, `ship2`) — they held
  licence copies. The helper scripts (`runstate.ps1`, `ashot.ps1`, `px2.ps1`, `region.ps1`,
  `uiadump.ps1`, `winshot.ps1`, `click.ps1`) stay.
* **P7–P10 ship done — all four variants (4 Nov 2026).**
  * **P7 Settings Overhaul:** full P1.8 design language, categorical sub-menus, dynamic toggle refresh, `← Back` navigation on all menus.
  * **P8 Inventory & Recipes:** full data model (`StockUnit`, `UnitMath`, `StockMovement`, `ProductStock`, extended `Ingredient` & `RecipeItem`), subforms (`IngredientManagerForm`, `RecipeEditorForm`, `StockManagerForm`), `StockEngine` unified deduction (sale, refund, partial payment), non-blocking `auto86Banner` & `● N low` app bar pill, `/api/stock` REST route, pizza worked scenario 100% verified, French & Arabic translations.
  * **P9 Product Images:** `CatalogImageStore` shared root resolution, bitmap/thumbnail caching, order-PC network path support.
  * **P10 Dual-Target Ship:**
    * Windows: `build-win.ps1 -Tfm all -Ship` compiled with 0 errors for both `net10.0-windows` and `net48`; binaries shipped to `publish-win10-x64\` and `publish-win7\` (`client database\` untouched). `tester-release\Windows\Vectron POS.exe` updated.
    * APK: `Vectron-Waiter-v1.2.0.apk` (28.7 MB) compiled and staged at repo root via `build-apk.ps1`; `tester-release\Android\Vectron-Waiter.apk` refreshed.
    * Release Zips: `VectronPOS-v1.2.0-Win10-x64.zip`, `…-Win7-x86-x64.zip`, `…-Win7-Full-x86-x64.zip`, and `tester-release.zip` all rebuilt entry-by-entry with `/` separators, starter catalog, 7 PNG images, 0 forbidden files.
* **Next work, if any:** only the Android defect backlog (§8 items 4–6) — nothing is blocking, and
  nothing is committed (`HEAD b00bb52`, working tree dirty by design).

---

## 10. Hard rules

* Never bypass or weaken the licence gate; never ship `license.dat` / `license.time` /
  `feature-settings.json` / `.pdb`.
* Never overwrite `client database\` in `publish-win10-x64`, `publish-win7` or `tester-release\Windows`.
* Never kill a running POS without checking whose it is — the user runs these live.
* Never commit or push unless the user explicitly asks.
* Never hardcode a colour: `Skin.*` on Windows, `App.xaml` keys + `Theme.C()` on Android.
* Never let a High-tier effect run in Lite — Lite must stay pixel- and cost-identical to the
  pre-P1.8 baseline.
* Never implement Light mode as an inversion — it is its own contrast-checked palette
  (accent `#0E7C63`, body text `#131821` on `#F3F6FA`).
* Never change the shipped defaults (Dark + Auto) without being asked.
* All four variants ship together, always.
