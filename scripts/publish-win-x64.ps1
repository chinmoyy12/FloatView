# scripts/publish-win-x64.ps1
$ErrorActionPreference = "Stop"

$DotnetRoot = if ($env:DOTNET_ROOT) { $env:DOTNET_ROOT } else { "$env:LOCALAPPDATA\Microsoft\dotnet" }
$env:DOTNET_ROOT = $DotnetRoot
$env:PATH = "$DotnetRoot;$env:PATH"

$PublishDir = "publish\win-x64"
Write-Host "==> Publishing self-contained win-x64 Release..." -ForegroundColor Cyan
dotnet publish src/FloatView.App/FloatView.App.csproj -c Release -r win-x64 --self-contained true -o $PublishDir

if (Test-Path "$PublishDir\FloatView.App.exe") {
    Write-Host "[SUCCESS] Published executable ready at $PublishDir\FloatView.App.exe" -ForegroundColor Green
} else {
    Write-Error "Published executable not found."
}
