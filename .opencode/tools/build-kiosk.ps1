<#
  build-kiosk.ps1 — build Vectron Kiosk (both TFMs) into publish-kiosk\

  Usage:
    .\build-kiosk.ps1         # build Win10 + Win7 into temp sandbox
    .\build-kiosk.ps1 -Ship   # ...and copy binaries into publish-kiosk\
#>
param(
    [switch]$Ship,
    [string]$OutRoot = "$env:LOCALAPPDATA\Temp\opencode\build"
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot\..\..").Path
$csproj = Join-Path $root 'publish-kiosk\src\VectronKiosk.csproj'
if (-not (Test-Path $csproj)) { $csproj = Join-Path $root 'VectronKiosk\VectronKiosk.csproj' }

$dotnet = 'C:\Users\serveur-it\.dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' }

$outWin10 = Join-Path $OutRoot 'kiosk_win10'
$outWin7 = Join-Path $OutRoot 'kiosk_win7'

Write-Host "`n=== publish net10.0-windows (Win10 x64 single-file) -> $outWin10 ===" -ForegroundColor Cyan
& $dotnet publish $csproj -c Release -f net10.0-windows -r win-x64 --self-contained true -p:PublishSingleFile=true -o $outWin10
if ($LASTEXITCODE -ne 0) { throw "Kiosk net10 publish failed" }

Write-Host "`n=== publish net48 (Win7) -> $outWin7 ===" -ForegroundColor Cyan
& $dotnet publish $csproj -c Release -f net48 -o $outWin7
if ($LASTEXITCODE -ne 0) { throw "Kiosk net48 publish failed" }

if ($Ship) {
    $dest = Join-Path $root 'publish-kiosk'
    if (-not (Test-Path $dest)) { New-Item -ItemType Directory -Path $dest | Out-Null }
    
    # 1. Win10 single-file in root of publish-kiosk
    Copy-Item (Join-Path $outWin10 'Vectron Kiosk.exe') (Join-Path $dest 'Vectron Kiosk.exe') -Force
    
    # 2. Images
    $imgSrc = Join-Path $root 'images\kiosk'
    $imgDst = Join-Path $dest 'images\kiosk'
    if (Test-Path $imgSrc) {
        if (-not (Test-Path $imgDst)) { New-Item -ItemType Directory -Path $imgDst | Out-Null }
        Copy-Item (Join-Path $imgSrc '*') $imgDst -Force
    }

    # 3. Win7 subfolder
    $destWin7 = Join-Path $dest 'Windows 7'
    if (-not (Test-Path $destWin7)) { New-Item -ItemType Directory -Path $destWin7 | Out-Null }
    Copy-Item (Join-Path $outWin7 'Vectron Kiosk.exe') (Join-Path $destWin7 'Vectron Kiosk.exe') -Force
    if (Test-Path (Join-Path $outWin7 'Vectron Kiosk.exe.config')) {
        Copy-Item (Join-Path $outWin7 'Vectron Kiosk.exe.config') (Join-Path $destWin7 'Vectron Kiosk.exe.config') -Force
    }
    # Net48 runtime dlls from publish-win7
    $win7Pub = Join-Path $root 'publish-win7'
    if (Test-Path $win7Pub) {
        Get-ChildItem -Path $win7Pub -Filter '*.dll' | ForEach-Object {
            Copy-Item $_.FullName (Join-Path $destWin7 $_.Name) -Force
        }
    }
    Write-Host "`nShipped fresh Kiosk binaries -> $dest" -ForegroundColor Green
}

Write-Host "`nDone." -ForegroundColor Green
