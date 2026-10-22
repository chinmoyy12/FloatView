$shell = New-Object -ComObject Shell.Application
$shell.ShellExecute("C:\Users\Public\Desktop\FloatView.lnk", "", "", "open", 1)
Start-Sleep -Seconds 3

$p = Get-Process FloatView.App -ErrorAction SilentlyContinue
if ($p) {
    Write-Host "LAUNCHED VIA EXPLORER: PID=$($p.Id), Title='$($p.MainWindowTitle)', Memory=$([math]::Round($p.WorkingSet64/1MB,1)) MB"
} else {
    Write-Host "Process not found."
}
