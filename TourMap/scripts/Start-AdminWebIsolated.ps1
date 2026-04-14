[CmdletBinding()]
param(
    [string]$Configuration = "Debug",
    [string]$Framework = "net10.0",
    [string]$Urls = "http://127.0.0.1:5042;http://[::1]:5042"
)

function Escape-PowerShellSingleQuotedString {
    param([string]$Value)

    return $Value.Replace("'", "''")
}

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$projectDir = Join-Path $repoRoot "TourMap.AdminWeb"
$projectFile = Join-Path $projectDir "TourMap.AdminWeb.csproj"
$runRoot = Join-Path $repoRoot ".run\TourMap.AdminWeb"
$buildRoot = Join-Path $runRoot "build"
$releaseRoot = Join-Path $runRoot "releases"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$releaseDir = Join-Path $releaseRoot $timestamp
$metadataPath = Join-Path $runRoot "current-run.json"
$launcherScriptPath = Join-Path $releaseDir "run-adminweb.ps1"
$consoleLog = Join-Path $releaseDir "console.log"

if (Test-Path $metadataPath) {
    $existingMetadata = Get-Content -Path $metadataPath -Raw | ConvertFrom-Json
    $existingProcess = Get-Process -Id $existingMetadata.ProcessId -ErrorAction SilentlyContinue
    if ($null -ne $existingProcess) {
        throw "An isolated TourMap.AdminWeb process is already running with PID $($existingProcess.Id). Stop it first with .\\scripts\\Stop-AdminWebIsolated.ps1."
    }

    Remove-Item -LiteralPath $metadataPath -ErrorAction SilentlyContinue
}

New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
New-Item -ItemType Directory -Path $buildRoot -Force | Out-Null

Write-Host "Publishing TourMap.AdminWeb to $releaseDir"
dotnet publish $projectFile `
    -c $Configuration `
    -f $Framework `
    -o $releaseDir `
    --artifacts-path $buildRoot

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$escapedReleaseDir = Escape-PowerShellSingleQuotedString $releaseDir
$escapedProjectDir = Escape-PowerShellSingleQuotedString $projectDir
$escapedUrls = Escape-PowerShellSingleQuotedString $Urls
$escapedConsoleLog = Escape-PowerShellSingleQuotedString $consoleLog

@"
Set-Location '$escapedReleaseDir'
& dotnet '.\TourMap.AdminWeb.dll' --environment Development --urls '$escapedUrls' '--TourMapPaths:ProjectRoot=$escapedProjectDir' *>> '$escapedConsoleLog'
"@ | Set-Content -Path $launcherScriptPath

$process = Start-Process `
    -FilePath "powershell" `
    -ArgumentList @("-NoLogo", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $launcherScriptPath) `
    -WorkingDirectory $releaseDir `
    -WindowStyle Minimized `
    -PassThru

$healthUrl = ($Urls -split ";")[0].Trim()
$healthUri = [Uri]$healthUrl
$isHealthy = $false

for ($attempt = 1; $attempt -le 15; $attempt++) {
    Start-Sleep -Seconds 1

    if ($process.HasExited) {
        break
    }

    try {
        $tcpClient = [System.Net.Sockets.TcpClient]::new()
        $tcpClient.Connect($healthUri.Host, $healthUri.Port)
        $tcpClient.Dispose()
        $isHealthy = $true
        break
    }
    catch {
        continue
    }
}

if (-not $isHealthy) {
    $consoleOutput = if (Test-Path $consoleLog) { Get-Content -Path $consoleLog -Raw } else { "" }
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -ErrorAction SilentlyContinue
    }

    throw "TourMap.AdminWeb did not become healthy at $healthUrl.`nConsole log:`n$consoleOutput"
}

[pscustomobject]@{
    ProcessId = $process.Id
    StartedAt = (Get-Date).ToString("o")
    ReleaseDir = $releaseDir
    Urls = $Urls
    LauncherScript = $launcherScriptPath
    ConsoleLog = $consoleLog
} | ConvertTo-Json | Set-Content -Path $metadataPath

Write-Host "TourMap.AdminWeb is running from $releaseDir"
Write-Host "PID: $($process.Id)"
Write-Host "URLs: $Urls"
Write-Host "Metadata: $metadataPath"
