# Ngrok Tunnel Script for TourMap
param([int]$Port = 5042)

# Check ngrok installed
try {
    $null = Get-Command ngrok -ErrorAction Stop
}
catch {
    Write-Host "Error: ngrok not found. Install with: choco install ngrok" -ForegroundColor Red
    exit 1
}

Write-Host "Starting ngrok tunnel to localhost:$Port..." -ForegroundColor Green
Write-Host ""
Write-Host "Copy the HTTPS URL below and update BackendEndpoints.cs:" -ForegroundColor Yellow
Write-Host ""

ngrok http http://localhost:$Port --host-header=localhost
