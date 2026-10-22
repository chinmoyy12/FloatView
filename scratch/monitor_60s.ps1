Write-Host "Directly starting D:\FloatView\FloatView.App.exe..."
$p = Start-Process -FilePath "D:\FloatView\FloatView.App.exe" -WorkingDirectory "D:\FloatView" -PassThru

Write-Host "Started PID $($p.Id). Monitoring for up to 60 seconds..."
for ($i = 1; $i -le 60; $i++) {
    Start-Sleep -Seconds 1
    $p.Refresh()
    if ($p.HasExited) {
        Write-Host "At second $($i): Process EXITED with ExitCode $($p.ExitCode)"
        break
    } else {
        if ($i % 5 -eq 0 -or $i -le 3) {
            Write-Host "At second $($i): RUNNING, WorkingSet=$([math]::Round($p.WorkingSet64/1MB, 1)) MB, Title='$($p.MainWindowTitle)'"
        }
    }
}
