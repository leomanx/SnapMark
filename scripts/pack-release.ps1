param([string]$SourceFolder = (Join-Path (Split-Path -Parent $PSScriptRoot) 'artifacts\SnapMark-portable'))
$ErrorActionPreference = 'Stop'
$packageRoot = (Resolve-Path -LiteralPath $SourceFolder).Path
$manifest = Get-Content -LiteralPath (Join-Path $packageRoot 'release-manifest.json') -Raw | ConvertFrom-Json
$archivePath = Join-Path (Split-Path -Parent $packageRoot) ('SnapMark-' + $manifest.version + '-win-x64.zip')
$temporaryArchive = $archivePath + '.' + [Guid]::NewGuid().ToString('N') + '.tmp'
try {
    $archive = [IO.Compression.ZipFile]::Open($temporaryArchive, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($entry in $manifest.files) {
            $path = Join-Path $packageRoot $entry.path
            if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $entry.sha256) { throw "Package hash mismatch: $($entry.path)" }
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $path, $entry.path.Replace('\','/'), [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
        foreach ($extra in @('release-manifest.json','portable.flag')) { [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, (Join-Path $packageRoot $extra), $extra) | Out-Null }
    } finally { $archive.Dispose() }
    Move-Item -LiteralPath $temporaryArchive -Destination $archivePath -Force
} finally { if (Test-Path -LiteralPath $temporaryArchive) { Remove-Item -LiteralPath $temporaryArchive } }
Write-Host "Release archive: $archivePath (excludes portable user data)"
$archiveHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
($archiveHash + '  ' + [IO.Path]::GetFileName($archivePath)) | Set-Content -LiteralPath ($archivePath + '.sha256') -Encoding ascii
