# Vectron POS

Restaurant point-of-sale suite by Vectron (client: Boitexinfo). Two applications sharing one
design language and one brand palette (navy `#131821`, panel `#1B222E`, mint `#47D6AE`).

| Application | Technology | Source | Ships as |
|---|---|---|---|
| **Vectron POS** — the till and the server | C# WinForms, dual target `net10.0-windows` + `net48` | `Program.cs` + 9 helper files | `VectronPOS-v1.2.0-Win10-x64.zip`, `…-Win7-x86-x64.zip`, `…-Win7-Full-x86-x64.zip` |
| **Vectron Waiter** — the tablet | .NET MAUI `net10.0-android` | `VectronTablet\` | `Vectron-Waiter-v1.2.0.apk` |

The POS owns the data and serves a REST API on port 8080 (auth = `?key=<ApiKey>`); the tablet is a
thin client on the same LAN.

## Appearance

Two user settings, present in both apps, stored per install:

- **Theme** — Dark / Light (Light is its own contrast-checked palette, not an inversion)
- **Graphics** — Auto / High / Lite (`Auto` detects the machine; `Lite` is the flat baseline with
  no effects and no timers, safe on Windows 7)

Defaults after install: **Dark + Auto**.

## Documentation

| File | Purpose |
|---|---|
| **`AGENTS.md`** | Orientation for AI agents and new developers — read this first. Includes the file-ownership map (which file owns the interface). |
| `ARCHITECTURE.md` | Full technical map: structure, `Skin`/`Theme` token APIs, data files, REST routes, build/ship commands, verification tooling and traps. |
| `PLAN.md` | Phased plan (P1 → P6) and the session-by-session log of what is done and what is next. |
| `DESIGN.md` | Both colour palettes, the Lite/High tier contract, the shell specification. |
| `MANUAL.md` / `MANUAL.pdf` | User manual (French). |

## Build

```powershell
# Windows, both TFMs
powershell -NoProfile -File ".opencode\tools\build-win.ps1" -Tfm all

# Android APK -> repo root Vectron-Waiter-v1.2.0.apk
powershell -NoProfile -File ".opencode\tools\build-apk.ps1"
```

The scripts resolve the toolchain (.NET SDK, JDK 17, Android SDK) themselves.

## Licence

Activation is offline and required — the app loops on the activation dialog until a valid key for
the machine fingerprint is entered. `license.dat` / `license.time` live in `client database\` and
are never distributed.
