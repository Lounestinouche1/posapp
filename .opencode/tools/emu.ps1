<#
  emu.ps1 — LDPlayer 9 control on THIS PC (adb-based; no window clicking needed).

  Usage:
    .\emu.ps1 launch                 # start instance 0 and wait for boot
    .\emu.ps1 quit                   # shut the instance down
    .\emu.ps1 shot  -Out file.png    # screenshot via adb exec-out screencap
    .\emu.ps1 tap   -X 640 -Y 400    # tap a point (screen pixels, 1280x720)
    .\emu.ps1 swipe -X 640 -Y 600 -X2 640 -Y2 200
    .\emu.ps1 text  -Value "hello"   # type ascii text into the focused field
    .\emu.ps1 key   -Code 4          # keyevent (4=back, 3=home, 66=enter)
    .\emu.ps1 install -Apk <path>    # adb install -r
    .\emu.ps1 start                  # launch com.vectron.waiter
    .\emu.ps1 ui                    # dump visible text (uiautomator)
    .\emu.ps1 log                   # recent app/errors logcat
#>
param(
    [Parameter(Mandatory = $true)][string]$Action,
    [string]$Out = "screenshot.png",
    [int]$X, [int]$Y, [int]$X2, [int]$Y2,
    [string]$Value, [int]$Code, [string]$Apk,
    [int]$Duration = 300
)

$ErrorActionPreference = 'Stop'
$ldRoot = 'C:\LDPlayer\LDPlayer9'
$ld = Join-Path $ldRoot 'ldconsole.exe'
$adb = Join-Path $ldRoot 'adb.exe'
$serial = 'emulator-5554'
$pkg = 'com.vectron.waiter'

function Ensure-Adb {
    $devices = & $adb devices 2>&1
    if ($devices -notmatch 'emulator-5554\s+device') {
        & $adb connect 127.0.0.1:5555 2>&1 | Out-Null
        Start-Sleep -Seconds 2
    }
}

switch ($Action) {
    'launch' {
        & $ld launch --index 0 | Out-Null
        for ($i = 0; $i -lt 40; $i++) {
            Start-Sleep -Seconds 3
            & $adb connect 127.0.0.1:5555 2>&1 | Out-Null
            $boot = (& $adb -s $serial shell getprop sys.boot_completed 2>&1) -join ''
            if ($boot.Trim() -eq '1') { Write-Host 'emulator booted'; break }
        }
    }
    'quit' { & $ld quit --index 0 }
    'shot' {
        Ensure-Adb
        # Byte-safe: pull the PNG from the device. A PowerShell ">" redirect re-encodes
        # stdout as UTF-16 and silently corrupts binary data (the file looks sized but
        # is not a valid PNG).
        $remote = '/sdcard/emu-shot.png'
        & $adb -s $serial shell screencap -p $remote | Out-Null
        if (Test-Path $Out) { Remove-Item $Out -Force }
        & $adb -s $serial pull $remote $Out 2>&1 | Out-Null
        & $adb -s $serial shell rm -f $remote | Out-Null
        Write-Host ("saved {0} ({1:N0} KB)" -f $Out, ((Get-Item $Out).Length / 1KB))
    }
    'tap' {
        Ensure-Adb
        & $adb -s $serial shell input tap $X $Y
        Write-Host "tapped $X,$Y"
    }
    'swipe' {
        Ensure-Adb
        & $adb -s $serial shell input swipe $X $Y $X2 $Y2 $Duration
        Write-Host "swiped $X,$Y -> $X2,$Y2 (${Duration}ms)"
    }
    'text' {
        Ensure-Adb
        $esc = $Value -replace ' ', '%s'
        & $adb -s $serial shell input text $esc
        Write-Host "typed: $Value"
    }
    'key' {
        Ensure-Adb
        & $adb -s $serial shell input keyevent $Code
        Write-Host "keyevent $Code"
    }
    'install' {
        Ensure-Adb
        & $adb -s $serial install -r $Apk
    }
    'start' {
        Ensure-Adb
        & $adb -s $serial shell monkey -p $pkg -c android.intent.category.LAUNCHER 1 2>&1 | Out-Null
        Write-Host "started $pkg"
    }
    'ui' {
        Ensure-Adb
        & $adb -s $serial shell uiautomator dump /sdcard/ui.xml 2>&1 | Out-Null
        $xml = (& $adb -s $serial shell cat /sdcard/ui.xml 2>&1) -join ''
        [regex]::Matches($xml, 'text="([^"]+)"') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique
    }
    'log' {
        Ensure-Adb
        & $adb -s $serial logcat -d -t 250 2>&1 | Select-String -Pattern 'FATAL|AndroidRuntime|Unhandled|com.vectron' | Select-Object -Last 20
    }
    default { throw "unknown action '$Action'" }
}
