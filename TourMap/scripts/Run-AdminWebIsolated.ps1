[CmdletBinding()]
param(
    [string]$Configuration = "Debug",
    [string]$Framework = "net10.0",
    [string]$Urls = "http://127.0.0.1:5042;http://[::1]:5042"
)

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

if (-not $env:DOTNET_CLI_HOME) {
    $env:DOTNET_CLI_HOME = Join-Path $repoRoot ".dotnet-cli"
}

if (-not $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE) {
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
}

if (-not $env:DOTNET_CLI_TELEMETRY_OPTOUT) {
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
}

New-Item -ItemType Directory -Path $env:DOTNET_CLI_HOME -Force | Out-Null
New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
New-Item -ItemType Directory -Path $buildRoot -Force | Out-Null

Write-Host "Publishing TourMap.AdminWeb for isolated Visual Studio launch..."
dotnet publish $projectFile `
    -c $Configuration `
    -f $Framework `
    -o $releaseDir `
    --artifacts-path $buildRoot

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Write-Host "Running TourMap.AdminWeb from $releaseDir"
Write-Host "URLs: $Urls"

Push-Location $releaseDir
try {
    & dotnet ".\TourMap.AdminWeb.dll" `
        --environment Development `
        --urls $Urls `
        "--TourMapPaths:ProjectRoot=$projectDir"
}
finally {
    Pop-Location
}
