<#
  build-win.ps1 — build the Windows POS (both TFMs) on THIS PC.

  Usage:
    .\build-win.ps1                  # build net10 + net48 into the temp sandbox (safe, no shipping)
    .\build-win.ps1 -Ship            # ...then copy the fresh binaries into publish-win10-x64\ and publish-win7\
    .\build-win.ps1 -Tfm net48       # build only one TFM

  Safety (PLAN §6): -Ship copies binaries ONLY. It never touches client database\, images\,
  license.dat, license.time or feature-settings.json in the target folders.
#>
param(
    [ValidateSet('all', 'net10', 'net48')][string]$Tfm = 'all',
    [switch]$Ship,
    [string]$OutRoot = "$env:LOCALAPPDATA\Temp\opencode\build"
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot\..\..").Path
if (-not (Test-Path (Join-Path $root 'VectronPOS.csproj'))) { $root = Split-Path $PSScriptRoot -Parent }
if (-not (Test-Path (Join-Path $root 'VectronPOS.csproj'))) { $root = 'F:\vscode projects\VectronPOS' }
$csproj = Join-Path $root 'VectronPOS.csproj'

# per-user toolchain (this PC has no admin, and C:\Program Files\dotnet is a broken stub)
$dotnet = 'C:\Users\serveur-it\.dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' }

$targets = switch ($Tfm) {
    'all'   { @(@{ Tfm = 'net10.0-windows'; Dir = 'win10'; Publish = 'publish-win10-x64' }, @{ Tfm = 'net48'; Dir = 'win7'; Publish = 'publish-win7' }) }
    'net10' { @(@{ Tfm = 'net10.0-windows'; Dir = 'win10'; Publish = 'publish-win10-x64' }) }
    'net48' { @(@{ Tfm = 'net48'; Dir = 'win7'; Publish = 'publish-win7' }) }
}

foreach ($t in $targets) {
    $out = Join-Path $OutRoot $t.Dir
    Write-Host "`n=== publish $($t.Tfm) -> $out ===" -ForegroundColor Cyan
    & $dotnet publish $csproj -f $t.Tfm -c Release -o $out
    if ($LASTEXITCODE -ne 0) { throw "publish $($t.Tfm) failed (exit $LASTEXITCODE)" }

    if ($Ship) {
        $dest = Join-Path $root $t.Publish
        if (-not (Test-Path $dest)) { throw "missing target folder: $dest" }
        $protected = @('client database', 'images', 'license.dat', 'license.time', 'feature-settings.json', 'api-settings.json')
        $copied = 0
        Get-ChildItem $out -File | Where-Object {
            $_.Extension -ne '.pdb' -and $protected -notcontains $_.Name
        } | ForEach-Object {
            $target = Join-Path $dest $_.Name
            if (Test-Path $target -PathType Container) { return }
            if (Test-Path $target) { Copy-Item $target "$target.old" -Force }
            Copy-Item $_.FullName $target -Force
            $copied++
        }
        Write-Host "  shipped $copied binaries -> $dest (client database untouched)" -ForegroundColor Green
    }
}

Write-Host "`nDone." -ForegroundColor Green
