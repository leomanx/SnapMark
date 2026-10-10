param([string]$OutputFolder)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputFolder) { $OutputFolder = Join-Path $projectRoot 'artifacts\SnapMark-portable' }
dotnet publish (Join-Path $projectRoot 'src\SnapMark.App\SnapMark.App.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $outputFolder
if ($LASTEXITCODE -ne 0) { throw 'Portable publish failed.' }
$buildProperties = [xml](Get-Content -LiteralPath (Join-Path $projectRoot 'Directory.Build.props') -Raw)
$targetFramework = [string]$buildProperties.Project.PropertyGroup.TargetFramework
$runtimeConfig = Get-Content -LiteralPath (Join-Path $projectRoot "src\SnapMark.App\bin\Release\$targetFramework\win-x64\SnapMark.runtimeconfig.json") -Raw | ConvertFrom-Json
foreach ($framework in $runtimeConfig.runtimeOptions.includedFrameworks) {
    $noticeName = if ($framework.name -eq 'Microsoft.NETCore.App') { 'DotNET-' + $framework.version + '-LICENSE.txt' } else { 'WindowsDesktop-' + $framework.version + '-LICENSE.txt' }
    if (-not (Test-Path -LiteralPath (Join-Path $projectRoot ('licenses\' + $noticeName)))) { throw "Refresh runtime license notices before publishing: $noticeName" }
}
New-Item -ItemType File -Path (Join-Path $outputFolder 'portable.flag') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot 'README.md') -Destination $outputFolder
$logoFolder = Join-Path $outputFolder 'src\SnapMark.App\Assets'
New-Item -ItemType Directory -Path $logoFolder -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot 'src\SnapMark.App\Assets\SnapMark.png') -Destination $logoFolder -Force
foreach ($name in @('LICENSE', 'CONTRIBUTING.md', 'SECURITY.md')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot $name) -Destination $outputFolder -Force
}
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs') -Destination $outputFolder -Recurse -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'licenses') -Destination $outputFolder -Recurse -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'extensions') -Destination $outputFolder -Recurse -Force
$exampleFolder = Join-Path $outputFolder 'examples\SnapMark.PngExporter'
New-Item -ItemType Directory -Path $exampleFolder -Force | Out-Null
foreach ($name in @('PngExporter.cs', 'SnapMark.PngExporter.csproj')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot ('examples\SnapMark.PngExporter\' + $name)) -Destination $exampleFolder -Force
}
dotnet build (Join-Path $projectRoot 'examples\SnapMark.PngExporter\SnapMark.PngExporter.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'Exporter example build failed.' }
Copy-Item -LiteralPath (Join-Path $projectRoot "examples\SnapMark.PngExporter\bin\Release\$targetFramework\SnapMark.PngExporter.dll") -Destination $exampleFolder -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'install-local.ps1') -Destination $OutputFolder
Write-Host "Portable app: $outputFolder\SnapMark.exe"
$version = (Get-Item -LiteralPath (Join-Path $OutputFolder 'SnapMark.exe')).VersionInfo.ProductVersion.Split('+')[0]
$rootPath = (Resolve-Path -LiteralPath $OutputFolder).Path
$files = @(Get-ChildItem -LiteralPath $rootPath -Recurse -File | Where-Object { $relative = [IO.Path]::GetRelativePath($rootPath, $_.FullName); $relative -notmatch '^(data[\\/]|portable.flag$|release-manifest.json$)' } | ForEach-Object { @{ path = [IO.Path]::GetRelativePath($rootPath, $_.FullName); sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash } })
@{ version = $version; files = $files } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $OutputFolder 'release-manifest.json') -Encoding utf8
