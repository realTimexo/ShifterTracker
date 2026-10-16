$ErrorActionPreference = 'Stop'
$version = 'v1.12.0'
$sha256 = 'cf03347a0bc88bfb198a606c0ff8375bc4a9420fa80c07ef336186986d79b244'
$uri = 'https://github.com/hifihedgehog/HIDMaestro/releases/download/v1.12.0/HIDMaestro-v1.12.0.zip'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$target = Join-Path $repo 'src\ShiftTracker\ThirdParty'
New-Item -ItemType Directory -Path $target -Force | Out-Null

$required = @(
    'HIDMaestro.Core.dll',
    'Microsoft.Windows.SDK.NET.dll',
    'WinRT.Runtime.dll',
    'HIDMaestro-LICENSE.txt',
    'HIDMaestro-THIRD-PARTY-NOTICES.txt'
)
if (($required | Where-Object { -not (Test-Path (Join-Path $target $_)) }).Count -eq 0) {
    Write-Host 'Pinned HIDMaestro runtime files already exist.'
    exit 0
}

$tempZip = Join-Path ([IO.Path]::GetTempPath()) 'HIDMaestro-v1.12.0.zip'
Write-Host "Downloading HIDMaestro $version build dependencies..."
Invoke-WebRequest -Uri $uri -OutFile $tempZip
$actual = (Get-FileHash -Path $tempZip -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actual -ne $sha256) {
    Remove-Item $tempZip -Force -ErrorAction SilentlyContinue
    throw "HIDMaestro archive checksum mismatch. Expected $sha256; got $actual."
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($tempZip)
try {
    $extract = @{
        'HIDMaestro.Core.dll' = 'HIDMaestro.Core.dll'
        'HIDMaestroTest\Microsoft.Windows.SDK.NET.dll' = 'Microsoft.Windows.SDK.NET.dll'
        'HIDMaestroTest\WinRT.Runtime.dll' = 'WinRT.Runtime.dll'
        'LICENSE' = 'HIDMaestro-LICENSE.txt'
        'THIRD-PARTY-NOTICES.txt' = 'HIDMaestro-THIRD-PARTY-NOTICES.txt'
    }
    foreach ($source in $extract.Keys) {
        $entry = $archive.Entries | Where-Object { $_.FullName -ceq $source } | Select-Object -First 1
        if ($null -eq $entry) { throw "Required file '$source' was not found in the pinned HIDMaestro archive." }
        $destination = Join-Path $target $extract[$source]
        $input = $entry.Open()
        try {
            $output = [IO.File]::Create($destination)
            try { $input.CopyTo($output) } finally { $output.Dispose() }
        } finally { $input.Dispose() }
    }
}
finally {
    $archive.Dispose()
    Remove-Item $tempZip -Force -ErrorAction SilentlyContinue
}
Write-Host 'Verified HIDMaestro runtime files extracted.'
