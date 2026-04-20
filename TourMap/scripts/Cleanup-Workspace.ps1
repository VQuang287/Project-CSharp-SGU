[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$workspaceRoot = [System.IO.Path]::GetFullPath($repoRoot)

$targets = @(
    ".run",
    "bin",
    "obj",
    "TourMap.AdminWeb\bin",
    "TourMap.AdminWeb\obj",
    ".tmpv",
    ".tmp_prd_vn",
    "_tmp_prd_blank_diagrams",
    "_tmp_prd.zip",
    "_tmp_prd_blank_diagrams.zip"
)

function Resolve-WorkspacePath {
    param([string]$RelativePath)

    return [System.IO.Path]::GetFullPath((Join-Path $workspaceRoot $RelativePath))
}

function Assert-InWorkspace {
    param([string]$FullPath)

    if (-not $FullPath.StartsWith($workspaceRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to delete path outside workspace: $FullPath"
    }
}

$removed = New-Object System.Collections.Generic.List[string]

foreach ($target in $targets) {
    $fullPath = Resolve-WorkspacePath -RelativePath $target
    Assert-InWorkspace -FullPath $fullPath

    if (-not (Test-Path -LiteralPath $fullPath)) {
        continue
    }

    $item = Get-Item -LiteralPath $fullPath -Force
    if ($item.PSIsContainer) {
        Remove-Item -LiteralPath $fullPath -Recurse -Force
    }
    else {
        Remove-Item -LiteralPath $fullPath -Force
    }

    $removed.Add($target) | Out-Null
}

if ($removed.Count -eq 0) {
    Write-Host "No cleanup targets found."
}
else {
    Write-Host "Removed:"
    $removed | ForEach-Object { Write-Host " - $_" }
}
