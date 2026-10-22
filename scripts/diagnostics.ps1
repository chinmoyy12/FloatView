# scripts/diagnostics.ps1
$ErrorActionPreference = "Continue"

Write-Host "=== FLOATVIEW SYSTEM DIAGNOSTICS ===" -ForegroundColor Cyan

# 1. .NET Runtime & SDK
Write-Host "`n[.NET Environment]" -ForegroundColor Yellow
$DotnetRoot = if ($env:DOTNET_ROOT) { $env:DOTNET_ROOT } else { "$env:LOCALAPPDATA\Microsoft\dotnet" }
$env:DOTNET_ROOT = $DotnetRoot
$env:PATH = "$DotnetRoot;$env:PATH"
dotnet --version
dotnet --list-runtimes

# 2. WebView2 Runtime Detection
Write-Host "`n[WebView2 Evergreen Runtime]" -ForegroundColor Yellow
$Keys = @(
    "HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}",
    "HKCU:\Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
)
$FoundWebView2 = $false
foreach ($k in $Keys) {
    if (Test-Path $k) {
        $pv = (Get-ItemProperty -Path $k -ErrorAction SilentlyContinue).pv
        if ($pv) {
            Write-Host "Detected WebView2 Evergreen Version: $pv ($k)" -ForegroundColor Green
            $FoundWebView2 = $true
            break
        }
    }
}
if (-not $FoundWebView2) {
    Write-Host "WebView2 Runtime not found in standard registry locations." -ForegroundColor Red
}

# 3. Displays & Monitors
Write-Host "`n[Windows Displays]" -ForegroundColor Yellow
Add-Type -AssemblyName System.Windows.Forms
[System.Windows.Forms.Screen]::AllScreens | ForEach-Object {
    Write-Host "Monitor: $($_.DeviceName) | Bounds: $($_.Bounds) | Primary: $($_.Primary) | WorkingArea: $($_.WorkingArea)"
}

# 4. Storage Directories
Write-Host "`n[FloatView Storage Paths]" -ForegroundColor Yellow
$AppData = "$env:APPDATA\FloatView"
$LocalAppData = "$env:LOCALAPPDATA\FloatView"
Write-Host "Config & Workspaces: $AppData (Exists: $(Test-Path $AppData))"
Write-Host "WebView2 Profile:    $LocalAppData\WebView2Profile (Exists: $(Test-Path "$LocalAppData\WebView2Profile"))"

Write-Host "`nDiagnostics Complete." -ForegroundColor Green
