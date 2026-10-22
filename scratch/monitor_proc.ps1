Write-Host "Directly starting D:\FloatView\FloatView.App.exe..."
$p = Start-Process -FilePath "D:\FloatView\FloatView.App.exe" -WorkingDirectory "D:\FloatView" -PassThru

Write-Host "Started PID $($p.Id). Monitoring for 15 seconds..."
for ($i = 1; $i -le 15; $i++) {
    Start-Sleep -Seconds 1
    $p.Refresh()
    if ($p.HasExited) {
        Write-Host "At second $($i): Process EXITED with ExitCode $($p.ExitCode)"
        break
    } else {
        Write-Host "At second $($i): RUNNING, WorkingSet=$([math]::Round($p.WorkingSet64/1MB, 1)) MB, Title='$($p.MainWindowTitle)'"
    }
}
