# scripts/package-installer.ps1
$ErrorActionPreference = "Stop"

$IsccPaths = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)

$Iscc = $IsccPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $Iscc) {
    Write-Error "Inno Setup compiler (ISCC.exe) was not found. Please install Inno Setup 6."
}

Write-Host "==> Compiling installer with $Iscc..." -ForegroundColor Cyan
& $Iscc "installer\FloatView.iss"

$InstallerOutput = "installer\output\FloatView-Setup-v1.0.0.exe"
if (Test-Path $InstallerOutput) {
    Write-Host "[SUCCESS] Windows Installer created at $InstallerOutput" -ForegroundColor Green
} else {
    Write-Error "Installer compilation did not produce expected output."
}
