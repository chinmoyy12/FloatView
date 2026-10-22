$procs = Get-Process FloatView.App -ErrorAction SilentlyContinue
if ($procs) {
    foreach ($p in $procs) {
        Write-Host "FOUND: Id=$($p.Id), Name=$($p.ProcessName), Title='$($p.MainWindowTitle)', Responding=$($p.Responding), MemoryMB=$([math]::Round($p.WorkingSet64/1MB, 2))"
    }
} else {
    Write-Host "No FloatView.App process running right now."
}
