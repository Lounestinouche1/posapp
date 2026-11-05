# APP.md — What we are building (read this first)

> **Purpose:** one authoritative brief about this product so *any* agent/session knows, without
> exploring, what the app is, what language it is written in, where it runs, how it is built,
> and which rules must never be broken.
>
> Auto-loaded via `opencode.json` → `instructions`. Keep it factual and up to date.
> **Day-to-day work status lives in `PLAN.md` (§9 = where work stopped). Design tokens live in `DESIGN.md`.**

---

## 1. What the product is

**Vectron POS Suite** — a restaurant point-of-sale (POS) product line built by **Vectron** for the
client **Boitexinfo**. It ships as **two applications that share one design language and one
on-premise data store**:

| # | App | What it does | Platform |
|---|---|---|---|
| 1 | **Vectron POS** | The operator terminal: orders, tables, payments, stock, staff, reports, kitchen display, printing, settings, licensing. The "brain" — it hosts the data store and the REST API. | Windows desktop (WinForms) |
| 2 | **Vectron Waiter** | The waiter companion: order at the table on a tablet, fire/bill, live alerts — talks to Vectron POS over LAN REST. | Android tablet (`.NET MAUI`) |

**Vendor:** Vectron · **Client:** Boitexinfo · **Current release:** v1.2.0 (Windows) / v1.2.0 (APK)
**Repo:** `https://github.com/Lounestinouche1/posapp.git` (root folder `D:\VectronPOS`)

### What it does (feature domain)
Restaurant/retail operations, mostly toggled by ~90 feature flags (`FeatureSettings`, `Program.cs`):

- **Service:** rooms & tables + floor plan, dine-in / takeaway / delivery, seats, courses, hold & fire,
  park/repeat orders, quick-sale mode, transfer/merge tables, split bills, tabs, gift cards, loyalty.
- **Money:** cash/card/other tenders, split & partial payment, tips, discounts & happy-hour rules,
  service charge / cash discount / card surcharge, cash rounding, refunds, X/Z end-of-day reports.
- **Menu:** products, families (categories), modifiers & modifier groups, notes, combos, variants,
  weighted items, images, product colors, barcodes (Code39) / QR (PDF + printed labels).
- **Stock:** ingredients, recipes, purchases & purchase orders, suppliers, stocktakes, waste,
  transfers, inventory alerts, low-stock / expiry, auto-86 when an ingredient runs out.
- **Staff:** operators with roles/PIN, clock in/out, shifts, wages & payroll export, schedule, audit log.
- **Kitchen:** kitchen display (KDS) with late alarm, 86 board, printer routing by destination.
- **Guest-facing:** QR table ordering web pages, customer-facing kiosk, guest alerts (waiter call /
  order ready), customer display — all served by the terminal itself.
- **Reporting:** sales/menu engineering, occupancy heatmap, discount/void report, live dashboard,
  AI insights, accounting export, receipts as PDF.
- **Infrastructure:** multi-terminal over LAN (Main PC + Order PCs), cloud sync/backup, multi-branch,
  multi-language with Arabic RTL, REST API + webhooks, OTA-free license enforcement.

---

## 2. Languages, frameworks and where it runs

### Tech stack

| | Vectron POS (terminal) | Vectron Waiter (tablet) |
|---|---|---|
| Language | **C#** (`LangVersion latest`, nullable enabled) | **C#** (same) |
| UI framework | **WinForms** (code-built UI, no designer files) | **`.NET MAUI`** (XAML + code-behind) |
| Target frameworks | **dual-TFM**: `net10.0-windows` (self-contained single-file x64) **+** `net48` (for Windows 7) | `net10.0-android` (single target) |
| Min platform | Windows 7 SP1 + .NET 4.8 (x86/x64), Windows 10/11 x64 | **Android 7.0 (API 24)** tablets |
| App id / exe | `Vectron POS.exe` (assembly name `Vectron POS`, root namespace `VectronPOS`) | `com.vectron.waiter`, display name **Vectron Waiter** |
| UI language | **3 languages**: `en` (default), `fr`, `ar` (RTL, `ar-MA`) via `Tr.T()` | English strings in XAML; shared design tokens |
| Dependencies | Only BCL + `System.Management`, `System.Text.Json` (net48) | `Microsoft.Maui.Controls`, `Microsoft.Extensions.Logging.Debug` |
| Architecture | Single-file app + JSON data store (no DB server) | MVVM-less pages/services, REST client |

**Ports in use:**
- `:8080` — embedded HTTP server: **REST API**, guest QR ordering pages, kiosk, invoice links.
- `:9100` — raw TCP **ESC/POS print server** (receipt printers on the LAN).

**Hard rule:** every UI/logic change must reach **all four ship variants** — Win10, Win7,
test release, Android. The test build is only for testing.

---

## 3. Repository map

```
D:\VectronPOS
├── Program.cs          ★ the ENTIRE Windows app (≈12,000 lines): data layer, forms, API, printing
├── Lang.cs             translations (en/fr/ar) + Tr.T() string table + UI re-styling helpers
├── License.cs          license gate + machine fingerprint   (LicenseForm.cs UI, LicenseGen\ = seller tool)
├── Code39.cs  QrCode.cs  TicketLayout.cs  TicketPdf (in Program.cs)   receipt/barcode/PDF helpers
├── Net48Polyfills.cs   Win7 / net48 compat shims (ECDSA ASN.1, SHA-256, Index/Range, LINQ)
├── PosCompat.cs        compatibility helpers
├── VectronPOS.csproj   dual-TFM build (net10.0-windows + net48)
├── VectronTablet\      ★ the Android MAUI app (excluded from the Windows build)
│   ├── Pages\          LoginPage, TablesPage, OrderPage, AlertsPage, ModifierSheetPage
│   ├── Services\       PosApi (REST client), OfflineQueue, AppState, AlertPolling/Job, ProductImages
│   ├── Theme.cs  App.xaml (design tokens)  Hud.cs (P1.7 Flat-HUD drawable)  MauiProgram.cs
│   └── Models\Dtos.cs  API DTOs
├── DESIGN.md           design language + tokens (both platforms) — authoritative for visuals
├── PLAN.md             ★ resume plan: what's done, what's next (§9 status), build commands (§1)
├── APP.md              ★ this file
├── MANUAL.md / .pdf    end-user manual (French market) + docs\images\ screenshots
├── publish-win10-x64\  publish-win7\  ← the user's LIVE installs (binaries only; DB never touched)
├── tester-release.zip  publish\  publish_self_contained\   build outputs
└── Vectron-Waiter-v1.2.0.apk   + VectronPOS-v1.2.0-*.zip  release artifacts in repo root
```

**Data store:** JSON files inside a folder named **`client database\`** next to the executable
(`PosStore` in `Program.cs`). Shared between terminals over SMB (`\\MAIN-PC\client database`).
Key files: `products.json`, `feature-settings.json` (`ApiEnabled`/`ApiKey`), operators, rooms,
sales, floor plan, printers, license, etc. **This folder is user data — never overwrite it.**

---

## 4. Networking / API (for tablet ↔ terminal work)

The terminal hosts `HttpListener`-style routing (`Program.cs`, ~line 7655+ → tablet API ~8159+).
API key required (`client database\feature-settings.json` → `ApiKey`, sent as `key`).

**Waiter tablet endpoints** (`/api/tablet/*`):
`bootstrap`, `login`, `menu`, `image`, `map`, `order` (GET/POST), `fire`, `bill`, `line-edit`,
`alerts`, `alerts/dismiss`.

**Other endpoints:** `/api/menu`, `/api/sales`, `/api/products`, `/api/orders` (GET + `key`),
`/api/status`, `/api/call`, `/api/order` (guest QR ordering) + inline HTML/JS for QR & kiosk pages.

Android client = `VectronTablet\Services\PosApi.cs`.

---

## 5. Design system (short version — full detail in `DESIGN.md`, rules in `PLAN.md` §3)

Dark navy + one mint accent, flat and data-dense by default. **Since 2 Nov (graphics overhaul,
PLAN §3) richer graphics and animations are ALLOWED — but every one of them is gated by a
Settings toggle, and the OFF path must cost exactly what today's build costs.**

- **Windows dark palette:** bg `#131821`, panel `#1B222E`, panel-light `#252F3E`, sidebar `#171D28`,
  mint `#47D6AE`, border/hairline `#2E3A4B`, pressed mint `#35B891`, warning amber `#F4B860`.
- **Android dark palette:** bg `#0F141A`, panel `#1A222C`, panel-2 `#243040`, mint `#70C1B3`,
  amber `#F4B860`, danger `#E07A5F`, green `#4E9F6E`, text `#E9EFF5`, dim `#8C99A8`.
- **Light palettes (new):** full hex sets for both platforms live in `PLAN.md` §3.3 and must be
  written into `DESIGN.md` before coding. Identity kept: navy sidebar + light canvas + mint
  accent; mint is a FILL on light, **`MintInk #0E8C71`** carries text/links (≥ 4.5:1).
- **Type:** Segoe UI (Windows, brand/totals scale with width), OpenSans (Android).
- **P1.7 "Flat-HUD" layer:** corner brackets (1px L-marks), 1px `#2E3A4B` hairlines, 3px mint
  accent rails, LED status dot, outline category chips, 2px focus rails, uppercased HUD micro-copy.
  Static geometry is always safe; glow/scan/blur/scanlines stay banned even at Motion=Full.
- **Settings (both apps):** `Theme: [Dark][Light]` + `Animations: [Off][Subtle][Full]` —
  Windows in `appearance.json`, Android in MAUI `Preferences`.

### Rules that apply to every UI change (PLAN §3.5)
The old "no animations" code ban is **replaced by a Settings toggle** (Animations = Off for
low-end PCs). Still non-negotiable even with animations ON:
1. No blur, no bloom, no per-pixel filters (`ColorMatrix`, `PathGradient` glows) — cheap static
   equivalents (cached gradients, hover rings) are fine.
2. No full-form `Invalidate()` on hot paths — repaint only the control that changed.
3. The motion timer **stops when the animation queue empties**; `Motion.Off` ⇒ no timer exists
   at all and every animation applies its final value instantly (pixel-identical to today).
4. No new NuGet/UI packages. Brushes/paths cached, never rebuilt inside `OnPaint`.
5. System fonts only (Segoe UI / OpenSans). **No icon fonts** — Segoe MDL2 doesn't exist on Win7
   and would break `net48`; keep the Unicode glyphs already in use (⚡ 🧾 🔥).
6. Must compile on **both TFMs** (net10.0-windows + net48) and run on Win7 SP1 + .NET 4.8.
7. Verify before shipping: build both TFMs 0 errors → run the `net48` build → PrintWindow
   screenshot → prove `Motion.Off` has no timer → only then package.

---

## 6. Build & run (canonical commands)

Full/updated commands live in **PLAN.md §1** — re-read it before building. Summary:

```powershell
# Windows — from any PC
dotnet publish "D:\VectronPOS\VectronPOS.csproj" -f net10.0-windows -c Release -o <dir>   # Win10
dotnet publish "D:\VectronPOS\VectronPOS.csproj" -f net48          -c Release -o <dir>   # Win7

# Android — machine-specific JAVA_HOME/SDK (see PLAN §1 for this PC's values)
dotnet publish "VectronTablet\VectronTablet.csproj" -f net10.0-android -c Release `
  -p:AndroidSdkDirectory="…" -p:JavaSdkDirectory="$env:JAVA_HOME"
# → VectronTablet\bin\Release\net10.0-android\com.vectron.waiter-Signed.apk
# → copy to repo root as Vectron-Waiter-v1.2.0.apk
```

**After any Windows build:** update `publish-win10-x64\` and `publish-win7\` **in place, binaries
only** (never their `client database\`), then restart the POS.

**Testing Android:** `F:\LDPlayer\LDPlayer14\ldconsole.exe installapp/runapp --index 0` —
generic adb is dead on the home PC; screenshots via PrintWindow (see PLAN §1).

---

## 7. Rules that must never be broken

1. **Never bypass or weaken the license gate** (`while (!License.IsActive)` in `Program.cs`).
   Two license keys per PC (one per TFM) is intentional — don't "fix" it.
2. **Never put** `license.dat`, `license.time`, `feature-settings.json`, `.pdb`, or user data in any
   shipping zip. Zips must contain `images\` (7 PNGs) + `client database\products.json`.
3. **Never overwrite `client database\`** in `publish-win10-x64\`, `publish-win7\`, or test dirs.
4. **Never kill a running POS process** without first checking whose it is — the user runs them live.
5. **Never commit/push unless explicitly asked.**
6. **Never use `Get-Content -Raw | Set-Content -Encoding UTF8`** on UTF-8 (no BOM) sources — it
   corrupts them; use `[System.IO.File]` APIs.
7. **All four variants ship together**, always.
8. Screenshots/findings must be cross-checked (PrintWindow + UI Automation) before being trusted.

---

## 8. Working conventions for agents

- **Start every session by reading `PLAN.md` fully** — §9 tells you exactly where work stopped.
  After a token/session reset, `RESUME.md` (auto-loaded, listed first) gives the one-line
  "Current state" pointer; verify it against PLAN.md §9, then continue.
- **When you stop (end of session or meaningful step): update BOTH** — `RESUME.md` →
  "Current state" (keep ≤ 70 lines) and `PLAN.md` §9 (full detail). This is what makes work
  survive daily token resets. Typing `/resume` in a fresh session re-runs the checklist.
- **Design questions → `DESIGN.md`** (tokens, components, Flat-HUD spec). Record new color/type
  decisions there.
- **Status updates → `PLAN.md` §9** after every meaningful step, so the next session can resume.
- Python is **broken** on the home PC (`py` points to a deleted install) — use PowerShell instead.
- Uncommitted work is normal and expected; do not clean it up, do not commit unless told.
