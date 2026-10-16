$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $repo 'src\ShiftTracker\ShiftTracker.csproj'
$version = [regex]::Match((Get-Content $project -Raw), '<Version>([^<]+)</Version>').Groups[1].Value
if (-not $version) { throw 'Could not read the application version from the project file.' }
& (Join-Path $PSScriptRoot 'fetch-hidmaestro.ps1')

$artifacts = Join-Path $repo 'artifacts'
$staging = Join-Path $artifacts 'windows-x64'
$zip = Join-Path $artifacts "ShiftTracker-v$version-win-x64.zip"
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory -Path $staging -Force | Out-Null

Write-Host "Publishing ShiftTracker $version for Windows x64..."
& dotnet publish $project -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:PublishTrimmed=false `
    -p:UseAppHost=true `
    -o $staging
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

Copy-Item (Join-Path $repo 'README.md') $staging
Copy-Item (Join-Path $repo 'GETTING-STARTED.txt') $staging
Copy-Item (Join-Path $repo 'CHANGELOG.md') $staging
Copy-Item (Join-Path $repo 'LICENSE') $staging
Copy-Item (Join-Path $repo 'THIRD-PARTY.md') $staging
Copy-Item (Join-Path $repo 'THIRD-PARTY-LICENSES\Apache-2.0.txt') $staging
Copy-Item (Join-Path $repo 'src\ShiftTracker\ThirdParty\HIDMaestro-LICENSE.txt') $staging
Copy-Item (Join-Path $repo 'src\ShiftTracker\ThirdParty\HIDMaestro-THIRD-PARTY-NOTICES.txt') $staging
if (Test-Path $zip) { Remove-Item $zip -Force }
$exeAsset = Join-Path $artifacts "ShiftTracker-v$version-win-x64.exe"
if (Test-Path $exeAsset) { Remove-Item $exeAsset -Force }
Copy-Item (Join-Path $staging 'ShiftTracker.exe') $exeAsset
$exeHash = (Get-FileHash -Path $exeAsset -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -Path "$exeAsset.sha256" -Value "$exeHash  $(Split-Path $exeAsset -Leaf)" -Encoding ascii
Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $zip -CompressionLevel Optimal
$hash = (Get-FileHash -Path $zip -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -Path "$zip.sha256" -Value "$hash  $(Split-Path $zip -Leaf)" -Encoding ascii
Write-Host "Release package: $zip"
Write-Host "SHA-256: $hash"
