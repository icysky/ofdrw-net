[CmdletBinding()]
param(
    [string]$ApiKey = $env:NUGET_API_KEY,
    [string]$Source = 'https://api.nuget.org/v3/index.json',
    [string]$PackageDirectory,
    [string]$Version = '0.1.1',
    [switch]$SkipDuplicate
)

$ErrorActionPreference = 'Stop'

if (-not $PackageDirectory) {
    $PackageDirectory = Join-Path (Split-Path -Parent $PSScriptRoot) 'artifacts\nuget'
}

if (-not $ApiKey) {
    $secure = Read-Host -AsSecureString 'nuget.org API key (Push scope)'
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try {
        $ApiKey = [Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

if (-not $ApiKey) {
    throw "A nuget.org API key is required."
}

$packages = Get-ChildItem $PackageDirectory -Filter "*.$Version.nupkg" -ErrorAction SilentlyContinue |
    Sort-Object Name

if (-not $packages) {
    throw "No packages found in $PackageDirectory. Run pack-nuget.ps1 first."
}

# Push modules first, meta package last.
$ordered = @($packages | Where-Object { $_.Name -ne "OfdNet.$Version.nupkg" }) +
           @($packages | Where-Object { $_.Name -eq "OfdNet.$Version.nupkg" })

foreach ($package in $ordered) {
    Write-Host "Pushing $($package.Name)..."
    $pushArgs = @('nuget', 'push', $package.FullName, '--api-key', $ApiKey, '--source', $Source)
    if ($SkipDuplicate) {
        $pushArgs += '--skip-duplicate'
    }

    & dotnet @pushArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Push failed for $($package.Name)"
    }
}

Write-Host ""
Write-Host "Published. Check: https://www.nuget.org/packages/OfdNet"
