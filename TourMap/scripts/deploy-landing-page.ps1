#!/usr/bin/env pwsh
# Script to deploy landing page and create GitHub Release with APK

param(
    [string]$ApkPath = "..\bin\Debug\net10.0-android\TourMap.apk",
    [string]$Version = "1.0.0",
    [string]$RepoOwner = "vquang287",
    [string]$RepoName = "tourmap-landing"
)

$ErrorActionPreference = "Stop"
Write-Host "🚀 TourMap Landing Page Deployment Script" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green

# Check if APK exists
if (-not (Test-Path $ApkPath)) {
    Write-Host "❌ APK not found at: $ApkPath" -ForegroundColor Red
    Write-Host "Please build the project first with: dotnet build -c Release" -ForegroundColor Yellow
    exit 1
}

$ApkSize = (Get-Item $ApkPath).Length / 1MB
Write-Host "📱 APK found: $ApkPath ($([math]::Round($ApkSize, 2)) MB)" -ForegroundColor Green

# Clone or update landing page repo
$LandingRepoPath = "d:\Code\$RepoName"
if (Test-Path $LandingRepoPath\.git) {
    Write-Host "🔄 Updating existing repo..." -ForegroundColor Blue
    Set-Location $LandingRepoPath
    git pull origin main
} else {
    Write-Host "📥 Cloning landing page repo..." -ForegroundColor Blue
    if (Test-Path $LandingRepoPath) {
        Remove-Item -Recurse -Force $LandingRepoPath
    }
    Set-Location d:\Code
    git clone "https://github.com/$RepoOwner/$RepoName.git" $RepoName
    Set-Location $LandingRepoPath
}

# Copy landing page HTML
Write-Host "📄 Copying landing page..." -ForegroundColor Blue
$SourceHtml = "d:\Code\ProjectCSharp-GitHub\TourMap\_docs\landing-page.html"
if (Test-Path $SourceHtml) {
    Copy-Item $SourceHtml -Destination "$LandingRepoPath\index.html" -Force
    
    # Update version in HTML
    $HtmlContent = Get-Content "$LandingRepoPath\index.html" -Raw
    $HtmlContent = $HtmlContent -replace 'v1\.0\.0', "v$Version"
    $HtmlContent = $HtmlContent -replace '85 MB', "$([math]::Round($ApkSize, 0)) MB"
    Set-Content "$LandingRepoPath\index.html" $HtmlContent -NoNewline
    
    Write-Host "✅ Landing page updated with version v$Version" -ForegroundColor Green
} else {
    Write-Host "⚠️ Source HTML not found, creating default..." -ForegroundColor Yellow
}

# Git operations
Write-Host "📝 Committing changes..." -ForegroundColor Blue
git add index.html
git commit -m "Update landing page to v$Version" -ErrorAction SilentlyContinue

# Push to GitHub
Write-Host "⬆️ Pushing to GitHub..." -ForegroundColor Blue
try {
    git push origin main
    Write-Host "✅ Landing page deployed!" -ForegroundColor Green
} catch {
    Write-Host "⚠️ Push failed. You may need to authenticate or resolve conflicts." -ForegroundColor Yellow
}

# Create GitHub Release instructions
Write-Host "" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host "🎉 DEPLOYMENT INSTRUCTIONS" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host "" -ForegroundColor White
Write-Host "1. Landing page deployed to: https://$RepoOwner.github.io/$RepoName/" -ForegroundColor Cyan
Write-Host "" -ForegroundColor White
Write-Host "2. To create GitHub Release with APK:" -ForegroundColor White
Write-Host "   a. Go to: https://github.com/$RepoOwner/$RepoName/releases" -ForegroundColor Yellow
Write-Host "   b. Click 'Draft a new release'" -ForegroundColor Yellow
Write-Host "   c. Tag version: v$Version" -ForegroundColor Yellow
Write-Host "   d. Release title: TourMap v$Version" -ForegroundColor Yellow
Write-Host "   e. Upload APK file from:" -ForegroundColor Yellow
Write-Host "      $ApkPath" -ForegroundColor Magenta
Write-Host "   f. Publish release" -ForegroundColor Yellow
Write-Host "" -ForegroundColor White
Write-Host "3. Update download link in index.html to match release URL" -ForegroundColor White
Write-Host "" -ForegroundColor Green
Write-Host "APK Location: $ApkPath" -ForegroundColor Magenta
Write-Host "APK Size: $([math]::Round($ApkSize, 2)) MB" -ForegroundColor Magenta
