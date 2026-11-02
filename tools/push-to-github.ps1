[CmdletBinding()]
param(
    [string]$Owner = 'icysky',
    [string]$Repo = 'ofdrw-net',
    [string]$Branch = 'main',
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$source = Split-Path -Parent $PSScriptRoot
$temp = Join-Path $env:TEMP 'ofdrw-net-github-export'

if (Test-Path $temp) {
    Remove-Item $temp -Recurse -Force
}

Write-Host "Exporting working tree to $temp ..."
# Exclude build outputs, NuGet artifacts, Git metadata and the commercial font.
& robocopy $source $temp /E /XD bin obj artifacts .git .vs .vscode /XF FangSong_GB2312.ttf | Out-Null
if ($LASTEXITCODE -ge 8) {
    throw "robocopy failed with exit code $LASTEXITCODE"
}

if (Get-ChildItem $temp -Recurse -Filter 'FangSong_GB2312.ttf') {
    throw "Commercial FangSong font was copied into the export. Aborting."
}

Push-Location $temp
try {
    git init -q
    git add -A
    git commit -m "chore: initial GitHub snapshot of ofdrwNet 0.1.0" | Out-Null
    git branch -M $Branch
    git remote add origin "https://github.com/$Owner/$Repo.git"

    $pushArgs = @('push', '-u', 'origin', $Branch)
    if ($Force) {
        $pushArgs += '--force'
    }

    & git @pushArgs
    if ($LASTEXITCODE -ne 0) {
        throw "git push failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
    Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Pushed to https://github.com/$Owner/$Repo ($Branch)"
