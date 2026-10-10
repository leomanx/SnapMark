param(
    [string]$SourceFolder = $(if (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'release-manifest.json')) { $PSScriptRoot } else { Join-Path (Split-Path -Parent $PSScriptRoot) 'artifacts\SnapMark-portable' }),
    [string]$DestinationFolder = (Join-Path $env:LOCALAPPDATA 'SnapMark'),
    [switch]$NoShortcuts,
    [switch]$DesktopShortcut
)
$ErrorActionPreference = 'Stop'
$sourceRoot = (Resolve-Path -LiteralPath $SourceFolder).Path
$installRoot = [IO.Path]::GetFullPath($DestinationFolder)
$manifest = Get-Content -LiteralPath (Join-Path $sourceRoot 'release-manifest.json') -Raw | ConvertFrom-Json
$releaseVersion = $null
if (-not [Version]::TryParse([string]$manifest.version, [ref]$releaseVersion)) { throw 'Invalid package version.' }
if (@($manifest.files).Count -eq 0 -or @($manifest.files).Count -gt 2000) { throw 'Invalid package file list.' }
$names = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($entry in $manifest.files) {
    $name = [string]$entry.path
    if ([IO.Path]::IsPathRooted($name) -or $name.Split([char[]]'\/') -contains '..' -or $name -match '[<>:"|?*]' -or $name -match '(^|[\\/])(data|portable.flag)([\\/]|$)' -or -not $names.Add($name)) { throw "Unsafe package path: $name" }
    $item = Get-Item -LiteralPath (Join-Path $sourceRoot $name)
    if ($item.PSIsContainer -or $item.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Package links/directories are not supported: $name" }
    if ((Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256).Hash -ne $entry.sha256) { throw "Package hash mismatch: $name" }
}
if (-not $names.Contains('SnapMark.exe')) { throw 'SnapMark.exe is missing from the package.' }
$actualVersion = [Version](Get-Item -LiteralPath (Join-Path $sourceRoot 'SnapMark.exe')).VersionInfo.ProductVersion.Split('+')[0]
if ($actualVersion -ne $releaseVersion) { throw 'Executable version does not match package manifest.' }
New-Item -ItemType Directory -Path $installRoot -Force | Out-Null
$versionFolder = Join-Path $installRoot ('version-' + $releaseVersion)
if (Test-Path -LiteralPath $versionFolder) {
    foreach ($entry in $manifest.files) { if (-not (Test-Path -LiteralPath (Join-Path $versionFolder $entry.path)) -or (Get-FileHash -LiteralPath (Join-Path $versionFolder $entry.path)).Hash -ne $entry.sha256) { throw 'An existing version folder differs from this package. Choose a new destination; no files were replaced.' } }
} else {
    $stage = Join-Path $installRoot ('staging-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $stage | Out-Null
    foreach ($entry in $manifest.files) { $target = Join-Path $stage $entry.path; New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null; Copy-Item -LiteralPath (Join-Path $sourceRoot $entry.path) -Destination $target }
    Copy-Item -LiteralPath (Join-Path $sourceRoot 'release-manifest.json') -Destination $stage
    Move-Item -LiteralPath $stage -Destination $versionFolder
}
if (-not $NoShortcuts) {
    $shellObject = New-Object -ComObject WScript.Shell
    $locations = @((Join-Path ([Environment]::GetFolderPath('StartMenu')) 'Programs\SnapMark.lnk'))
    if ($DesktopShortcut) { $locations += Join-Path ([Environment]::GetFolderPath('Desktop')) 'SnapMark.lnk' }
    foreach ($location in $locations) {
        $shortcut = $shellObject.CreateShortcut($location)
        if ((Test-Path -LiteralPath $location) -and -not $shortcut.TargetPath.StartsWith($installRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw "Existing shortcut points outside this installation: $location" }
        $shortcut.TargetPath = Join-Path $versionFolder 'SnapMark.exe'; $shortcut.IconLocation = $shortcut.TargetPath + ',0'; $shortcut.WorkingDirectory = $versionFolder; $shortcut.Description = 'SnapMark local capture and markup'; $shortcut.Save()
    }
}
Write-Host "Installed $releaseVersion at $versionFolder"
Write-Host 'Run the same script with a new local package to update. Older version folders remain available. Installed-mode data stays in %APPDATA%\SnapMark; portable data is not copied.'
