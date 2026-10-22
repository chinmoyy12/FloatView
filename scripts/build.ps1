# scripts/build.ps1
$ErrorActionPreference = "Stop"

$DotnetRoot = if ($env:DOTNET_ROOT) { $env:DOTNET_ROOT } else { "$env:LOCALAPPDATA\Microsoft\dotnet" }
$env:DOTNET_ROOT = $DotnetRoot
$env:PATH = "$DotnetRoot;$env:PATH"

Write-Host "==> Verifying .NET SDK..." -ForegroundColor Cyan
dotnet --version

Write-Host "==> Restoring dependencies..." -ForegroundColor Cyan
dotnet restore FloatView.slnx

Write-Host "==> Building solution (Release)..." -ForegroundColor Cyan
dotnet build FloatView.slnx -c Release --no-restore

Write-Host "[SUCCESS] FloatView Release Build Succeeded." -ForegroundColor Green
