$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$sdkRoot = Join-Path $env:LOCALAPPDATA "Android\Sdk"
$logDir = Join-Path $projectRoot "logs"
New-Item -ItemType Directory -Path $logDir -Force | Out-Null
$logFile = Join-Path $logDir "run-android.log"
"[$(Get-Date -Format o)] Starting run-android.ps1" | Out-File -FilePath $logFile -Encoding utf8

function Write-Log {
    param([string]$Message)
    $line = "[$(Get-Date -Format o)] $Message"
    Write-Host $line
    Add-Content -Path $logFile -Value $line
}

function Ensure-Adb {
    param([string]$SdkRoot)

    $adbCandidates = @(
        (Join-Path $SdkRoot "platform-tools\adb.exe"),
        "C:\Android\sdk\platform-tools\adb.exe",
        "C:\Program Files\Android\Android Studio\platform-tools\adb.exe"
    )

    $adb = $adbCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if ($adb) {
        return $adb
    }

    Write-Log "adb.exe not found. Bootstrapping Android Platform-Tools..."
    New-Item -ItemType Directory -Path $SdkRoot -Force | Out-Null

    $zipPath = Join-Path $env:TEMP "platform-tools-latest-windows.zip"
    $downloadUrl = "https://dl.google.com/android/repository/platform-tools-latest-windows.zip"

    Invoke-WebRequest -Uri $downloadUrl -OutFile $zipPath
    Expand-Archive -Path $zipPath -DestinationPath $SdkRoot -Force
    Remove-Item $zipPath -Force -ErrorAction SilentlyContinue

    $downloadedAdb = Join-Path $SdkRoot "platform-tools\adb.exe"
    if (-not (Test-Path $downloadedAdb)) {
        throw "Failed to install Android Platform-Tools (adb.exe missing after extraction)."
    }

    Write-Log "Installed adb at: $downloadedAdb"
    return $downloadedAdb
}

function Invoke-AdbLogged {
    param(
        [string]$AdbPath,
        [string[]]$Arguments
    )

    $previousErrorAction = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & $AdbPath @Arguments 2>&1
        if ($output) {
            $output | ForEach-Object {
                $line = $_.ToString()
                Write-Host $line
                Add-Content -Path $logFile -Value $line
            }
        }
        return $output
    }
    finally {
        $ErrorActionPreference = $previousErrorAction
    }
}

try {
    Set-Location $projectRoot

    $env:ANDROID_SDK_ROOT = $sdkRoot
    $env:ANDROID_HOME = $sdkRoot

    $adbExe = Ensure-Adb -SdkRoot $sdkRoot
    $platformTools = Split-Path -Parent $adbExe
    if ($env:Path -notlike "*$platformTools*") {
        $env:Path += ";$platformTools"
    }

    Write-Log "Project root: $projectRoot"
    Write-Log "Android SDK root: $sdkRoot"
    Write-Log "ADB: $adbExe"

    Write-Log "Running: adb start-server"
    Invoke-AdbLogged -AdbPath $adbExe -Arguments @("start-server") | Out-Null

    Write-Log "Running: adb devices (poll up to 30s)"
    $deadline = (Get-Date).AddSeconds(30)
    $hasConnectedDevice = $false
    $hasUnauthorized = $false
    $hasOffline = $false

    do {
        $deviceOutput = Invoke-AdbLogged -AdbPath $adbExe -Arguments @("devices")
        if ($LASTEXITCODE -ne 0) {
            Write-Log "adb devices returned exit code $LASTEXITCODE, retrying..."
            Start-Sleep -Seconds 2
            continue
        }

        $hasUnauthorized = $false
        $hasOffline = $false
        $hasConnectedDevice = $false

        if ($deviceOutput) {
            foreach ($lineObj in $deviceOutput) {
                $line = $lineObj.ToString().Trim()
                if ($line -match "\sdevice$") { $hasConnectedDevice = $true }
                if ($line -match "\sunauthorized$") { $hasUnauthorized = $true }
                if ($line -match "\soffline$") { $hasOffline = $true }
            }
        }

        if ($hasConnectedDevice) { break }
        Start-Sleep -Seconds 2
    } while ((Get-Date) -lt $deadline)

    if (-not $hasConnectedDevice) {
        if ($hasUnauthorized) {
            throw "Device is unauthorized. Unlock phone and tap 'Allow USB debugging', then rerun task."
        }
        if ($hasOffline) {
            throw "Device is offline. Replug USB cable, set File Transfer mode, then rerun task."
        }
        throw "No Android device detected by adb. On Windows, install the correct USB driver (Samsung/Xiaomi/OEM or Google USB Driver), then reconnect phone with File Transfer and USB debugging enabled."
    }

    Write-Log "Running: dotnet restore TourMap.csproj"
    dotnet restore TourMap.csproj 2>&1 | Tee-Object -FilePath $logFile -Append | Out-Host

    Write-Log "Running: dotnet build TourMap.csproj -f net10.0-android -t:InstallAndroidDependencies"
    dotnet build TourMap.csproj -f net10.0-android -t:InstallAndroidDependencies -p:AndroidSdkDirectory=$sdkRoot -p:AcceptAndroidSdkLicenses=True 2>&1 | Tee-Object -FilePath $logFile -Append | Out-Host

    Write-Log "Running: dotnet build TourMap.csproj -f net10.0-android -c Debug"
    dotnet build TourMap.csproj -f net10.0-android -c Debug -p:AndroidSdkDirectory=$sdkRoot 2>&1 | Tee-Object -FilePath $logFile -Append | Out-Host

    Write-Log "Running: dotnet build TourMap.csproj -f net10.0-android -t:Run -c Debug"
    dotnet build TourMap.csproj -f net10.0-android -t:Run -c Debug -p:AndroidSdkDirectory=$sdkRoot 2>&1 | Tee-Object -FilePath $logFile -Append | Out-Host

    Write-Log "Done"
}
catch {
    Write-Log "FAILED: $($_.Exception.Message)"
    Write-Log "See log file: $logFile"
    exit 1
}
