# scripts/register-extension-host.ps1
# Registers FloatView's Native Messaging Host manifest for Chrome and Edge
$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$AppExe = Join-Path $ProjectRoot "publish\win-x64\FloatView.App.exe"
if (-not (Test-Path $AppExe)) {
    $AppExe = Join-Path $ProjectRoot "src\FloatView.App\bin\Release\net10.0-windows\FloatView.App.exe"
}
if (-not (Test-Path $AppExe)) {
    $AppExe = Join-Path $ProjectRoot "src\FloatView.App\bin\Debug\net10.0-windows\FloatView.App.exe"
}

$ManifestDir = Join-Path $ProjectRoot "extension"
$ManifestPath = Join-Path $ManifestDir "com.floatview.bridge.json"

Write-Host "==> Registering FloatView Native Messaging Host..." -ForegroundColor Cyan
Write-Host "    Target Exe: $AppExe"
Write-Host "    Manifest:   $ManifestPath"

# Ensure manifest has the absolute path to executable
$ManifestContent = @"
{
  "name": "com.floatview.bridge",
  "description": "FloatView Smart Media Detection Bridge",
  "path": "$($AppExe.Replace('\', '\\'))",
  "type": "stdio",
  "allowed_origins": [
    "chrome-extension://*/*"
  ]
}
"@

[System.IO.File]::WriteAllText($ManifestPath, $ManifestContent)

# Registry paths for Chrome and Edge (HKCU requires no administrator rights)
$RegistryTargets = @(
    "HKCU:\Software\Google\Chrome\NativeMessagingHosts\com.floatview.bridge",
    "HKCU:\Software\Microsoft\Edge\NativeMessagingHosts\com.floatview.bridge"
)

foreach ($regPath in $RegistryTargets) {
    $parent = Split-Path -Parent $regPath
    if (-not (Test-Path $parent)) {
        New-Item -Path $parent -ItemType Directory -Force | Out-Null
    }
    New-Item -Path $regPath -ItemType Directory -Force | Out-Null
    Set-ItemProperty -Path $regPath -Name "(Default)" -Value $ManifestPath
    Write-Host "    [OK] Registered in: $regPath" -ForegroundColor Green
}

Write-Host "[SUCCESS] FloatView Native Messaging Host successfully registered." -ForegroundColor Green
