# AGENTS.md — read this first

**TL POS** — restaurant and retail POS suite by Lounes Tinouche (TL). Two apps, one design
language, same brand colours. This file is the orientation page for any AI agent or developer
opening the repository. It is deliberately short.

## Read order

1. **`AGENTS.md`** (this file) — what the project is, which file owns what, hard rules.
2. **`ARCHITECTURE.md`** — the full map: structure, `Skin`/`Theme` token APIs, data files,
   REST routes, build + ship commands, verification tooling and its traps, defect list.
3. **`PLAN.md`** — phased plan (P1.1 → P6) and the session-by-session log of what is done.
   **§10 is the current work** (P7 Settings, P8 inventory/recipes, P9 images, P10 ship).
4. **`DESIGN.md`** — both palettes, the Lite/High tier contract, the shell spec, and
   **§"Implementation contract — building a new screen"** — the mandatory recipe book for any
   new screen (intent→role colours, geometry, typography, component recipes, blueprints,
   anti-drift self-check).

Do not start editing before reading 1 + 2. `PLAN.md` §9 is long; skim the last section only.

## Which file is responsible for the interface

This is the single most important fact: **the entire Windows UI lives in one file.**

### Windows POS — C# WinForms, dual target `net10.0-windows` + `net48`

| Concern | File |
|---|---|
| **Every screen and every control** (main window, login, operator picker, settings, product/room/family/printer managers, reports, kitchen display, on-screen keyboard, all dialogs) | `Program.cs` |
| **Colour tokens** — `internal static class Skin` (search `class Skin`) | `Program.cs` (~line 966) |
| Main window + shell (`PosForm`, app bar, nav pills, order card) — search `class PosForm`, `void BuildUi` | `Program.cs` |
| Product tile card (`ProductTile`) — search `class ProductTile` | `Program.cs` |
| REST API on port 8080 (`QrOrderServer`) — search `class QrOrderServer` | `Program.cs` |
| Data layer + JSON persistence (`PosStore`) — search `class PosStore` | `Program.cs` |
| Printing, tickets, PDF (`RawPrinter`, `TicketPdf`) | `Program.cs` + `TicketLayout.cs` |
| Translations, en / fr / ar (`Tr.T("key")`) | `Lang.cs` |
| Licence gate (ECDSA, **never bypass**) / activation dialog | `License.cs` / `LicenseForm.cs` |
| Ticket designer form + layout model | `TicketLayout.cs` |
| Barcode / QR renderers (no external packages) | `Code39.cs` / `QrCode.cs` |
| `net48` compatibility shims | `Net48Polyfills.cs`, `PosCompat.cs` |
| Build targets | `VectronPOS.csproj` |

`Program.cs` is ~13.5k lines and strictly layered top-to-bottom. **Search by type name, not by
line number** — line numbers shift on every edit.

### Android tablet — .NET MAUI `net10.0-android` (folder `VectronTablet\`)

| Concern | File |
|---|---|
| **The only place colours are declared** (tokens + shared styles) | `VectronTablet\App.xaml` |
| Runtime appearance layer: `Apply()`, `SetTheme`/`SetQuality`, `Restyle()`, `Reveal`/`Depth`/`Press` | `VectronTablet\Theme.cs` |
| Main shell: rooms + tables (and the `...` Appearance menu) | `VectronTablet\Pages\TablesPage.xaml(.cs)` |
| Order screen: product grid, basket, qty stepper, send to kitchen | `VectronTablet\Pages\OrderPage.xaml(.cs)` |
| Login / alerts / modifier sheet | `VectronTablet\Pages\LoginPage.*`, `AlertsPage.*`, `ModifierSheetPage.cs` |
| Every REST call the tablet makes | `VectronTablet\Services\PosApi.cs` |
| Session, cart, feature flags, offline queue | `VectronTablet\Services\AppState.cs`, `OfflineQueue.cs` |
| HUD corner-bracket drawable | `VectronTablet\Hud.cs` |
| DI + the Android button-tint fix | `VectronTablet\MauiProgram.cs` |
| Status/navigation bar colours | `VectronTablet\Platforms\Android\MainActivity.cs` |

**Rule:** a colour may only be written in `Program.cs`'s `Skin` class (Windows) or
`VectronTablet\App.xaml` / `Theme.cs` (Android). No raw hex in a page, no `Color.FromArgb(...)`
in new code.

## The two appearance settings (both apps)

- **Theme = Dark / Light** — Light is its own contrast-checked palette, never an inversion.
- **Graphics = Auto / High / Lite** — `Auto` detects the machine; `High` adds gradients, soft
  depth and 150 ms transitions; `Lite` is the flat baseline with **zero** effects and zero timers.
- Defaults after install: **Dark + Auto**.
- **Never let a High-tier effect run on Lite** — every gradient / shadow / transition must sit
  behind `Quality == High` (Windows) or `Theme.IsHigh` (Android).

## Hard rules

- Never bypass or weaken the licence gate. Never ship `license.dat`, `license.time`,
  `feature-settings.json` or `.pdb` inside a zip.
- Never overwrite `client database\` in `publish-win10-x64\`, `publish-win7\` or
  `tester-release\Windows` — those are live installs with customer data.
- Never kill a running POS process without checking whose it is; the user runs these live.
- Never commit or push unless the user explicitly asks.
- Every change must reach **all four variants**: Win10, Win7, tester build, Android.
- Everything must compile for **both** `net10.0-windows` and `net48`; Lite must run on Win7 SP1
  with .NET 4.8.
- **Any new screen, dialog or control follows `DESIGN.md`'s "Implementation contract"** — its
  intent→role colour table, geometry scale, typography scale and component recipes. No invented
  colours, fonts, radii or spacing; run its 10-point anti-drift self-check before calling a
  screen done.

## Build

```powershell
# Windows, both TFMs -> %LOCALAPPDATA%\Temp\opencode\build\win10 | win7
powershell -NoProfile -File ".opencode\tools\build-win.ps1" -Tfm all

# Android -> repo root Vectron-Waiter-v1.2.0.apk
powershell -NoProfile -File ".opencode\tools\build-apk.ps1"
```

The helper scripts resolve the toolchain themselves (SDK, JDK, Android SDK paths). On the DEV PC
`C:\Program Files\dotnet` is a broken stub ahead of PATH — use the scripts or PowerShell, never
plain `dotnet` from `cmd.exe`.

## Verification

You cannot trust a screenshot you cannot see: use UI Automation (`uiadump.ps1`) for Windows layout
and pixel probes (`px2.ps1`) for colour. On Android use `adb shell screencap -p /sdcard/x.png`
followed by `adb pull` — **never** `adb exec-out ... > file.png`, because PowerShell's `>` writes
UTF-16 and silently produces a corrupt PNG. Details and expected colour values: `ARCHITECTURE.md` §7.

## Where the work stands

P1 → P6 are complete and shipped to all four variants (4 Nov 2026). Remaining backlog is
Android-only and non-blocking: product tiles fire on press (a swipe can add an item), basket rows
can slip under the fixed bottom bar, draft basket is dropped on back-navigation.
See `ARCHITECTURE.md` §8–§9 before changing anything.
