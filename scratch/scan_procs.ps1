$all = Get-Process
foreach ($proc in $all) {
    if ($proc.ProcessName -like "*Float*") {
        Write-Host "MATCH: $($proc.Id) - $($proc.ProcessName) - '$($proc.MainWindowTitle)'"
    }
}
Write-Host "Done scanning all processes."
