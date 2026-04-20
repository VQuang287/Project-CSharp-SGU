[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$metadataPath = Join-Path $repoRoot ".run\TourMap.AdminWeb\current-run.json"

if (-not (Test-Path $metadataPath)) {
    Write-Host "No isolated AdminWeb run metadata was found."
    exit 0
}

$metadata = Get-Content -Path $metadataPath -Raw | ConvertFrom-Json
$process = Get-Process -Id $metadata.ProcessId -ErrorAction SilentlyContinue

if ($null -eq $process) {
    Write-Host "Process $($metadata.ProcessId) is already stopped."
}
else {
    Stop-Process -Id $process.Id
    Write-Host "Stopped TourMap.AdminWeb process $($process.Id)."
}

Remove-Item -LiteralPath $metadataPath -ErrorAction SilentlyContinue
