# Vectron POS — Windows 10 & 11 Terminal (x64)

Self-contained Windows 10/11 desktop Point of Sale application.

## Overview
- **Binary:** `Vectron POS.exe` (~117 MB single-file standalone executable).
- **Runtime:** Fully self-contained .NET 10 runtime embedded (zero prerequisites required on Windows 10/11).
- **Target Framework:** `net10.0-windows`
- **Source Code:** Main project files at repository root ([`Program.cs`](../Program.cs), [`VectronPOS.csproj`](../VectronPOS.csproj)).

## How to Build Locally
Run the build script from the repository root:
```powershell
powershell -NoProfile -File ".opencode\tools\build-win.ps1" -Tfm net10 -Ship
```
This compiles the application and outputs the ready-to-run `Vectron POS.exe` directly into this folder.

## Folder Structure
- `Vectron POS.exe` — Main executable.
- `client database\` — Local store configuration, tables, menu, operators, and ticket data (kept local and protected).
- `images\` — Product photos and category thumbnails.

## Pre-built Releases
Pre-packaged release ZIPs can be downloaded directly from the GitHub repository **Releases** tab without needing to compile.
