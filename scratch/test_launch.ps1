$sh = New-Object -ComObject WScript.Shell
$sc = $sh.CreateShortcut('C:\Users\Public\Desktop\FloatView.lnk')
Write-Host "Target: $($sc.TargetPath)"
Write-Host "WorkingDir: $($sc.WorkingDirectory)"

$stdoutFile = "C:\Users\chinmoy\.gemini\antigravity-ide\scratch\FloatView\scratch\stdout.log"
$stderrFile = "C:\Users\chinmoy\.gemini\antigravity-ide\scratch\FloatView\scratch\stderr.log"

Remove-Item $stdoutFile -ErrorAction SilentlyContinue
Remove-Item $stderrFile -ErrorAction SilentlyContinue

$p = Start-Process -FilePath $sc.TargetPath -WorkingDirectory $sc.WorkingDirectory -RedirectStandardOutput $stdoutFile -RedirectStandardError $stderrFile -PassThru

Write-Host "Started process $($p.Id). Monitoring for 5 seconds..."
for ($i = 0; $i -lt 5; $i++) {
    Start-Sleep -Seconds 1
    $p.Refresh()
    Write-Host "Second $($i+1): HasExited=$($p.HasExited)"
    if ($p.HasExited) {
        Write-Host "Process exited early with ExitCode: $($p.ExitCode)"
        break
    }
}

if (Test-Path $stdoutFile) {
    Write-Host "--- STDOUT ---"
    Get-Content $stdoutFile
}
if (Test-Path $stderrFile) {
    Write-Host "--- STDERR ---"
    Get-Content $stderrFile
}
