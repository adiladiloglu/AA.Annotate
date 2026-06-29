param(
    [string]$InstallRoot = (Join-Path $env:LOCALAPPDATA 'AA.Annotate'),
    [string]$SkillsRoot = (Join-Path $env:USERPROFILE '.codex\skills'),
    [string]$PluginsRoot = (Join-Path $env:USERPROFILE 'plugins'),
    [string]$MarketplacePath = (Join-Path $env:USERPROFILE '.agents\plugins\marketplace.json'),
    [switch]$AddCliToUserPath,
    [switch]$SetUserAppEnvironmentVariable,
    [switch]$InstallCodexPlugin
)

$ErrorActionPreference = 'Stop'

function New-DefaultMarketplace {
    [ordered]@{
        name = 'personal'
        interface = [ordered]@{
            displayName = 'Personal'
        }
        plugins = @()
    }
}

function Get-PluginMarketplaceEntry {
    [ordered]@{
        name = 'aa-annotate'
        source = [ordered]@{
            source = 'local'
            path = './plugins/aa-annotate'
        }
        policy = [ordered]@{
            installation = 'AVAILABLE'
            authentication = 'ON_INSTALL'
        }
        category = 'Developer Tools'
    }
}

function Set-CodexMarketplaceEntry {
    param(
        [string]$Path
    )

    if (Test-Path -LiteralPath $Path) {
        $marketplace = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
        if ($null -eq $marketplace -or $marketplace -isnot [psobject]) {
            throw "Codex marketplace file must contain a JSON object: $Path"
        }
    }
    else {
        $marketplace = New-DefaultMarketplace
    }

    if ($null -eq $marketplace.plugins) {
        $marketplace | Add-Member -NotePropertyName plugins -NotePropertyValue @()
    }

    if ($marketplace.plugins -isnot [array]) {
        $marketplace.plugins = @($marketplace.plugins)
    }

    $entry = Get-PluginMarketplaceEntry
    $plugins = @($marketplace.plugins | Where-Object { $_.name -ne 'aa-annotate' })
    $marketplace.plugins = @($plugins + $entry)

    $marketplaceFolder = Split-Path -Parent $Path
    New-Item -ItemType Directory -Path $marketplaceFolder -Force | Out-Null
    $marketplace | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Install-CodexPlugin {
    param(
        [string]$PackageRoot,
        [string]$PluginsRoot,
        [string]$MarketplacePath
    )

    $pluginManifestSource = Join-Path $PackageRoot '.codex-plugin'
    if (-not (Test-Path -LiteralPath $pluginManifestSource)) {
        throw "Package is incomplete. Missing: $pluginManifestSource"
    }

    $pluginTarget = Join-Path $PluginsRoot 'aa-annotate'
    if (Test-Path -LiteralPath $pluginTarget) {
        Remove-Item -LiteralPath $pluginTarget -Recurse -Force
    }

    New-Item -ItemType Directory -Path $pluginTarget -Force | Out-Null

    foreach ($itemName in @('.codex-plugin', '.claude-plugin', 'app', 'cli', 'skills', 'manifest.json', 'README.txt')) {
        $source = Join-Path $PackageRoot $itemName
        if (Test-Path -LiteralPath $source) {
            Copy-Item -LiteralPath $source -Destination (Join-Path $pluginTarget $itemName) -Recurse
        }
    }

    Set-CodexMarketplaceEntry -Path $MarketplacePath

    $pluginCli = Join-Path $pluginTarget 'cli\aa-annotate.exe'
    $pluginManifest = Join-Path $pluginTarget '.codex-plugin\plugin.json'
    foreach ($requiredPath in @($pluginCli, $pluginManifest)) {
        if (-not (Test-Path -LiteralPath $requiredPath)) {
            throw "Codex plugin install verification failed. Missing: $requiredPath"
        }
    }

    [ordered]@{
        Root = $pluginTarget
        Manifest = $pluginManifest
        CLI = $pluginCli
        Marketplace = $MarketplacePath
    }
}

$packageRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$appSource = Join-Path $packageRoot 'app'
$cliSource = Join-Path $packageRoot 'cli'
$skillSource = Join-Path $packageRoot 'skills\aa-annotate'

foreach ($requiredPath in @($appSource, $cliSource, $skillSource)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Package is incomplete. Missing: $requiredPath"
    }
}

$appTarget = Join-Path $InstallRoot 'app'
$cliTarget = Join-Path $InstallRoot 'cli'
$skillTarget = Join-Path $SkillsRoot 'aa-annotate'

New-Item -ItemType Directory -Path $InstallRoot -Force | Out-Null
New-Item -ItemType Directory -Path $SkillsRoot -Force | Out-Null

foreach ($target in @($appTarget, $cliTarget, $skillTarget)) {
    if (Test-Path -LiteralPath $target) {
        Remove-Item -LiteralPath $target -Recurse -Force
    }
}

Copy-Item -LiteralPath $appSource -Destination $appTarget -Recurse
Copy-Item -LiteralPath $cliSource -Destination $cliTarget -Recurse
Copy-Item -LiteralPath $skillSource -Destination $skillTarget -Recurse

$cliExe = Join-Path $cliTarget 'aa-annotate.exe'
$appExe = Join-Path $appTarget 'AA.Annotate.App.exe'

if ($AddCliToUserPath) {
    $currentPath = [Environment]::GetEnvironmentVariable('PATH', 'User')
    if ($null -eq $currentPath) {
        $currentPath = ''
    }

    $pathEntries = $currentPath -split ';' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    if ($pathEntries -notcontains $cliTarget) {
        [Environment]::SetEnvironmentVariable('PATH', (($pathEntries + $cliTarget) -join ';'), 'User')
    }
}

if ($SetUserAppEnvironmentVariable) {
    [Environment]::SetEnvironmentVariable('AA_ANNOTATE_APP', $appExe, 'User')
}

$pluginInstall = $null
if ($InstallCodexPlugin) {
    $pluginInstall = Install-CodexPlugin -PackageRoot $packageRoot -PluginsRoot $PluginsRoot -MarketplacePath $MarketplacePath
}

Write-Host "AA Annotate installed."
Write-Host "App:   $appExe"
Write-Host "CLI:   $cliExe"
Write-Host "Skill: $skillTarget"
if ($InstallCodexPlugin) {
    Write-Host "Plugin: $($pluginInstall.Root)"
    Write-Host "Marketplace: $($pluginInstall.Marketplace)"
}
Write-Host ""
Write-Host "Run without PATH changes:"
Write-Host "  & `"$cliExe`" session --wait"
Write-Host ""
Write-Host "Optional Codex plugin install:"
Write-Host "  .\install.ps1 -InstallCodexPlugin"
Write-Host ""
Write-Host "Optional user-scoped registration:"
Write-Host "  .\install.ps1 -AddCliToUserPath"
Write-Host "  .\install.ps1 -SetUserAppEnvironmentVariable"
