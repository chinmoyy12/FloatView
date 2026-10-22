Write-Host "Launching C:\Users\Public\Desktop\FloatView.lnk..."
$p = Start-Process -FilePath "C:\Users\Public\Desktop\FloatView.lnk" -PassThru
Start-Sleep -Seconds 4

$procs = Get-Process FloatView.App -ErrorAction SilentlyContinue
if ($procs) {
    foreach ($proc in $procs) {
        Write-Host "SUCCESS: FloatView.App is RUNNING! [PID: $($proc.Id), Title: '$($proc.MainWindowTitle)', Memory: $([math]::Round($proc.WorkingSet64/1MB, 1)) MB, Responding: $($proc.Responding)]"
    }
} else {
    Write-Host "ERROR: FloatView.App is not running."
    if (Test-Path "$env:APPDATA\FloatView\fatal.log") {
        Write-Host "--- FATAL LOG ---"
        Get-Content "$env:APPDATA\FloatView\fatal.log"
    }
}
