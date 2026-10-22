# scripts/test.ps1
$ErrorActionPreference = "Stop"

$DotnetRoot = if ($env:DOTNET_ROOT) { $env:DOTNET_ROOT } else { "$env:LOCALAPPDATA\Microsoft\dotnet" }
$env:DOTNET_ROOT = $DotnetRoot
$env:PATH = "$DotnetRoot;$env:PATH"

Write-Host "==> Running FloatView Test Suite..." -ForegroundColor Cyan
dotnet test tests/FloatView.Tests/FloatView.Tests.csproj --verbosity normal
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] FloatView Unit Tests Failed." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "[SUCCESS] All FloatView Unit Tests Passed." -ForegroundColor Green
