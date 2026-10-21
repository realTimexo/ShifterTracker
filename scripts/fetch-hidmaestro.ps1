$ErrorActionPreference = 'Stop'
$version = 'v2.0.0'
$sha256 = '9678ca1e7d2b689af99d353677403f644f9948bd0553598c953f80e4333a8479'
$coreSha256 = '8fe7bdb21ec497591f527b7b87c4bc7b41b45c35a06cc211a7a0dce48838ed2c'
$uri = 'https://github.com/hifihedgehog/HIDMaestro/releases/download/v2.0.0/HIDMaestro-v2.0.0.zip'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$target = Join-Path $repo 'src\ShiftTracker\ThirdParty'
New-Item -ItemType Directory -Path $target -Force | Out-Null
$stamp = Join-Path $target 'HIDMaestro-Version.txt'
$core = Join-Path $target 'HIDMaestro.Core.dll'
$license = Join-Path $target 'HIDMaestro-LICENSE.txt'
$notices = Join-Path $target 'HIDMaestro-THIRD-PARTY-NOTICES.txt'
if ((Test-Path $stamp) -and (Get-Content $stamp -Raw).Trim() -eq $version -and (Test-Path $core) -and (Get-FileHash $core -Algorithm SHA256).Hash.ToLowerInvariant() -eq $coreSha256 -and (Test-Path $license) -and (Test-Path $notices)) {
    Write-Host "Verified HIDMaestro $version files already exist."
    exit 0
}

$tempZip = Join-Path ([IO.Path]::GetTempPath()) 'HIDMaestro-v2.0.0.zip'
Write-Host "Downloading HIDMaestro $version build dependency (large upstream release archive)..."
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
$actualCore = (Get-FileHash -Path $core -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualCore -ne $coreSha256) { throw "HIDMaestro.Core.dll checksum mismatch. Expected $coreSha256; got $actualCore." }
Set-Content -Path $stamp -Value $version -NoNewline -Encoding ascii
Write-Host "HIDMaestro $version archive and Core assembly verified and extracted."
