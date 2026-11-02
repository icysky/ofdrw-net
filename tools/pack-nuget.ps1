[CmdletBinding()]
param(
    [string]$Version = '0.1.1',
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'Ofd.slnx'

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $root 'artifacts\nuget'
}

if (Test-Path $OutputDirectory) {
    Remove-Item $OutputDirectory -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

Write-Host "Packing OfdNet $Version..."
& dotnet pack $solution -c Release -o $OutputDirectory -p:Version=$Version
if ($LASTEXITCODE -ne 0) {
    throw "dotnet pack failed with exit code $LASTEXITCODE"
}

$expected = @(
    'OfdNet',
    'OfdNet.Core',
    'OfdNet.Pkg',
    'OfdNet.Reader',
    'OfdNet.Crypto',
    'OfdNet.Sign',
    'OfdNet.Gm',
    'OfdNet.Font',
    'OfdNet.Layout',
    'OfdNet.Archive',
    'OfdNet.Tool'
)

$packages = Get-ChildItem $OutputDirectory -Filter *.nupkg | Sort-Object Name
if ($packages.Count -ne $expected.Count) {
    throw "Expected $($expected.Count) packages, found $($packages.Count)."
}

Add-Type -AssemblyName System.IO.Compression.FileSystem

foreach ($name in $expected) {
    $package = $packages | Where-Object { $_.Name -like "$name.$Version.nupkg" } | Select-Object -First 1
    if (-not $package) {
        throw "Missing package: $name.$Version.nupkg"
    }

    $zip = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)
    try {
        $entries = @($zip.Entries | ForEach-Object { $_.FullName })

        foreach ($required in @('LICENSE', 'NOTICE', 'README.md', 'THIRD-PARTY-NOTICES.md', 'PORTING-NOTES.md')) {
            if ($entries -notcontains $required) {
                throw "$($package.Name) is missing $required"
            }
        }

        if ($entries -match 'FangSong_GB2312\.ttf') {
            throw "$($package.Name) contains the commercial FangSong font."
        }

        if ($name -eq 'OfdNet.Font') {
            foreach ($required in @(
                'contentFiles/any/any/Assets/NotoSerif/NotoSerifSC-Regular.ttf',
                'contentFiles/any/any/Assets/NotoSerif/NotoSerifSC-Bold.ttf',
                'contentFiles/any/any/Assets/NotoSerif/OFL.txt')) {
                if ($entries -notcontains $required) {
                    throw "OfdNet.Font is missing $required"
                }
            }
        }

        if ($name -eq 'OfdNet') {
            $nuspecEntry = $zip.Entries | Where-Object { $_.FullName -like '*.nuspec' } | Select-Object -First 1
            $reader = New-Object System.IO.StreamReader($nuspecEntry.Open())
            try {
                $nuspec = $reader.ReadToEnd()
            }
            finally {
                $reader.Dispose()
            }

            $dependencyCount = ([regex]::Matches($nuspec, '<dependency id="OfdNet\.')).Count
            if ($dependencyCount -ne 10) {
                throw "OfdNet meta package should depend on 10 modules, found $dependencyCount."
            }
        }
    }
    finally {
        $zip.Dispose()
    }

    Write-Host ("OK  {0,-28} {1,8:N2} MB" -f $package.Name, ($package.Length / 1MB))
}

Write-Host ""
Write-Host "Packages are ready in: $OutputDirectory"
