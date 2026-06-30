param(
    [string]$Runtime = 'win-x64',
    [string]$Configuration = 'Release',
    [string]$OutputRoot = 'artifacts\dist',
    [string]$Version = '0.1.0',
    [bool]$SelfContained = $true
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$packageName = "aa-annotate-$Version-$Runtime"
$packageRoot = Join-Path (Join-Path $repoRoot $OutputRoot) $packageName
$publishRoot = Join-Path $repoRoot 'artifacts\publish'
$appPublish = Join-Path $publishRoot "app-$Runtime"
$cliPublish = Join-Path $publishRoot "cli-$Runtime"

foreach ($path in @($packageRoot, $appPublish, $cliPublish)) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}

New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null

$selfContainedText = if ($SelfContained) { 'true' } else { 'false' }
$publishArgs = @(
    '-c', $Configuration,
    '-r', $Runtime,
    '--self-contained', $selfContainedText,
    '-p:PublishSingleFile=false'
)

dotnet publish (Join-Path $repoRoot 'src\AA.Annotate.App\AA.Annotate.App.csproj') @publishArgs -o $appPublish
dotnet publish (Join-Path $repoRoot 'src\AA.Annotate.Cli\AA.Annotate.Cli.csproj') @publishArgs -o $cliPublish

Copy-Item -LiteralPath $appPublish -Destination (Join-Path $packageRoot 'app') -Recurse
Copy-Item -LiteralPath $cliPublish -Destination (Join-Path $packageRoot 'cli') -Recurse

$skillsTarget = Join-Path $packageRoot 'skills'
New-Item -ItemType Directory -Path $skillsTarget -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot 'skills\aa-annotate') -Destination (Join-Path $skillsTarget 'aa-annotate') -Recurse

Copy-Item -LiteralPath (Join-Path $repoRoot 'packaging\windows\install.ps1') -Destination (Join-Path $packageRoot 'install.ps1')
Copy-Item -LiteralPath (Join-Path $repoRoot 'packaging\windows\uninstall.ps1') -Destination (Join-Path $packageRoot 'uninstall.ps1')
Copy-Item -LiteralPath (Join-Path $repoRoot 'packaging\windows\README.txt') -Destination (Join-Path $packageRoot 'README.txt')
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination (Join-Path $packageRoot 'LICENSE')

$claudePluginSource = Join-Path $repoRoot '.claude-plugin'
if (Test-Path -LiteralPath $claudePluginSource) {
    Copy-Item -LiteralPath $claudePluginSource -Destination (Join-Path $packageRoot '.claude-plugin') -Recurse
}

$codexPluginSource = Join-Path $repoRoot '.codex-plugin'
if (Test-Path -LiteralPath $codexPluginSource) {
    Copy-Item -LiteralPath $codexPluginSource -Destination (Join-Path $packageRoot '.codex-plugin') -Recurse
}

$manifest = [ordered]@{
    name = 'aa-annotate'
    version = $Version
    packageKind = 'app-skill-bundle'
    license = 'Apache-2.0'
    runtime = $Runtime
    selfContained = $SelfContained
    createdAtUtc = [DateTimeOffset]::UtcNow.ToString('O')
    appExecutable = 'app/AA.Annotate.App.exe'
    cliExecutable = 'cli/aa-annotate.exe'
    skill = 'skills/aa-annotate'
    install = 'install.ps1'
    uninstall = 'uninstall.ps1'
    defaultInstallRoot = '%LOCALAPPDATA%/AA.Annotate'
    defaultSkillPath = '%USERPROFILE%/.codex/skills/aa-annotate'
    pluginMetadata = '.codex-plugin/plugin.json'
    claudePluginMetadata = '.claude-plugin/plugin.json'
    codexPluginMetadata = '.codex-plugin/plugin.json'
}

$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $packageRoot 'manifest.json') -Encoding UTF8

$archivePath = Join-Path (Join-Path $repoRoot $OutputRoot) "$packageName.zip"
if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory(
    $packageRoot,
    $archivePath,
    [System.IO.Compression.CompressionLevel]::Optimal,
    $false)

if (-not (Test-Path -LiteralPath $archivePath)) {
    throw "Package archive was not created: $archivePath"
}

Write-Host "Package folder: $packageRoot"
Write-Host "Package archive: $archivePath"
