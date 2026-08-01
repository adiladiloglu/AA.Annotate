param(
    [string]$Runtime = 'linux-x64',
    [string]$Configuration = 'Release',
    [string]$OutputRoot = 'artifacts/dist',
    [string]$Version = '0.5.0',
    [bool]$SelfContained = $true
)

$ErrorActionPreference = 'Stop'

if (-not $IsLinux) {
    throw 'Linux packages must be created on Linux so tar records executable Unix file modes. Use the Linux release job or a Linux development machine.'
}

if (-not $Runtime.StartsWith('linux-', [StringComparison]::OrdinalIgnoreCase)) {
    throw "The Linux package script only accepts Linux runtime identifiers. Received: $Runtime"
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$packageName = "aa-annotate-$Version-$Runtime"
$distRoot = Join-Path $repoRoot $OutputRoot
$packageRoot = Join-Path $distRoot $packageName
$publishRoot = Join-Path $repoRoot 'artifacts/publish'
$appPublish = Join-Path $publishRoot "app-$Runtime"
$cliPublish = Join-Path $publishRoot "cli-$Runtime"
$archivePath = Join-Path $distRoot "$packageName.tar.gz"

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
    '-p:PublishSingleFile=false',
    "-p:Version=$Version",
    "-p:AssemblyVersion=$Version.0",
    "-p:FileVersion=$Version.0",
    "-p:InformationalVersion=$Version"
)

dotnet publish (Join-Path $repoRoot 'src/AA.Annotate.App/AA.Annotate.App.csproj') @publishArgs -o $appPublish
if ($LASTEXITCODE -ne 0) {
    throw 'Publishing the Linux desktop app failed.'
}

dotnet publish (Join-Path $repoRoot 'src/AA.Annotate.Cli/AA.Annotate.Cli.csproj') @publishArgs -o $cliPublish
if ($LASTEXITCODE -ne 0) {
    throw 'Publishing the Linux CLI failed.'
}

Copy-Item -LiteralPath $appPublish -Destination (Join-Path $packageRoot 'app') -Recurse
Copy-Item -LiteralPath $cliPublish -Destination (Join-Path $packageRoot 'cli') -Recurse

$skillsTarget = Join-Path $packageRoot 'skills'
New-Item -ItemType Directory -Path $skillsTarget -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot 'skills/aa-annotate') -Destination (Join-Path $skillsTarget 'aa-annotate') -Recurse

Copy-Item -LiteralPath (Join-Path $repoRoot 'packaging/linux/install.sh') -Destination (Join-Path $packageRoot 'install.sh')
Copy-Item -LiteralPath (Join-Path $repoRoot 'packaging/linux/uninstall.sh') -Destination (Join-Path $packageRoot 'uninstall.sh')
Copy-Item -LiteralPath (Join-Path $repoRoot 'packaging/linux/README.txt') -Destination (Join-Path $packageRoot 'README.txt')
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination (Join-Path $packageRoot 'LICENSE')

foreach ($pluginFolder in @('.claude-plugin', '.codex-plugin')) {
    $source = Join-Path $repoRoot $pluginFolder
    if (Test-Path -LiteralPath $source) {
        Copy-Item -LiteralPath $source -Destination (Join-Path $packageRoot $pluginFolder) -Recurse
    }
}

$manifest = [ordered]@{
    name = 'aa-annotate'
    version = $Version
    packageKind = 'app-skill-bundle'
    license = 'Apache-2.0'
    runtime = $Runtime
    selfContained = $SelfContained
    appExecutable = 'app/AA.Annotate.App'
    cliExecutable = 'cli/aa-annotate'
    skill = 'skills/aa-annotate'
    install = 'install.sh'
    uninstall = 'uninstall.sh'
    defaultInstallRoot = '$HOME/.local/opt/aa-annotate'
    defaultSkillPath = '${CODEX_HOME:-$HOME/.codex}/skills/aa-annotate'
    pluginMetadata = '.codex-plugin/plugin.json'
    claudePluginMetadata = '.claude-plugin/plugin.json'
    codexPluginMetadata = '.codex-plugin/plugin.json'
}

$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $packageRoot 'manifest.json') -Encoding utf8NoBOM

$executablePaths = @(
    (Join-Path $packageRoot 'app/AA.Annotate.App'),
    (Join-Path $packageRoot 'cli/aa-annotate'),
    (Join-Path $packageRoot 'install.sh'),
    (Join-Path $packageRoot 'uninstall.sh')
)
$executeMode = [System.IO.UnixFileMode]::UserRead -bor
    [System.IO.UnixFileMode]::UserWrite -bor
    [System.IO.UnixFileMode]::UserExecute -bor
    [System.IO.UnixFileMode]::GroupRead -bor
    [System.IO.UnixFileMode]::GroupExecute -bor
    [System.IO.UnixFileMode]::OtherRead -bor
    [System.IO.UnixFileMode]::OtherExecute
foreach ($path in $executablePaths) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Linux package is incomplete. Missing executable: $path"
    }

    [System.IO.File]::SetUnixFileMode($path, $executeMode)
}

if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}

$tarCommand = Get-Command tar -ErrorAction SilentlyContinue
if ($null -eq $tarCommand) {
    throw 'The tar command is required to create the Linux package.'
}

& $tarCommand.Source -czf $archivePath -C $distRoot $packageName
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $archivePath -PathType Leaf)) {
    throw "Linux package archive was not created: $archivePath"
}

$archiveEntries = & $tarCommand.Source -tzf $archivePath
if ($LASTEXITCODE -ne 0) {
    throw "Linux package archive could not be read: $archivePath"
}

foreach ($requiredEntry in @(
    "$packageName/app/AA.Annotate.App",
    "$packageName/cli/aa-annotate",
    "$packageName/skills/aa-annotate/SKILL.md",
    "$packageName/install.sh",
    "$packageName/uninstall.sh",
    "$packageName/manifest.json",
    "$packageName/LICENSE"
)) {
    if ($archiveEntries -notcontains $requiredEntry) {
        throw "Linux package archive is missing: $requiredEntry"
    }
}

Write-Host "Package folder: $packageRoot"
Write-Host "Package archive: $archivePath"
