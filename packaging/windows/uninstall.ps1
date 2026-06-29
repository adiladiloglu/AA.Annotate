param(
    [string]$InstallRoot = (Join-Path $env:LOCALAPPDATA 'AA.Annotate'),
    [string]$SkillsRoot = (Join-Path $env:USERPROFILE '.codex\skills'),
    [string]$PluginsRoot = (Join-Path $env:USERPROFILE 'plugins'),
    [string]$MarketplacePath = (Join-Path $env:USERPROFILE '.agents\plugins\marketplace.json'),
    [switch]$RemoveCliFromUserPath,
    [switch]$RemoveUserAppEnvironmentVariable,
    [switch]$RemoveCodexPlugin
)

$ErrorActionPreference = 'Stop'

$cliTarget = Join-Path $InstallRoot 'cli'
$skillTarget = Join-Path $SkillsRoot 'aa-annotate'
$pluginTarget = Join-Path $PluginsRoot 'aa-annotate'

if (Test-Path -LiteralPath $InstallRoot) {
    Remove-Item -LiteralPath $InstallRoot -Recurse -Force
}

if (Test-Path -LiteralPath $skillTarget) {
    Remove-Item -LiteralPath $skillTarget -Recurse -Force
}

if ($RemoveCodexPlugin) {
    if (Test-Path -LiteralPath $pluginTarget) {
        Remove-Item -LiteralPath $pluginTarget -Recurse -Force
    }

    if (Test-Path -LiteralPath $MarketplacePath) {
        $marketplace = Get-Content -LiteralPath $MarketplacePath -Raw | ConvertFrom-Json
        if ($null -ne $marketplace.plugins) {
            $marketplace.plugins = @($marketplace.plugins | Where-Object { $_.name -ne 'aa-annotate' })
            $marketplace | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $MarketplacePath -Encoding UTF8
        }
    }
}

if ($RemoveCliFromUserPath) {
    $currentPath = [Environment]::GetEnvironmentVariable('PATH', 'User')
    if ($null -eq $currentPath) {
        $currentPath = ''
    }

    $pathEntries = $currentPath -split ';' |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and $_ -ne $cliTarget }
    [Environment]::SetEnvironmentVariable('PATH', ($pathEntries -join ';'), 'User')
}

if ($RemoveUserAppEnvironmentVariable) {
    [Environment]::SetEnvironmentVariable('AA_ANNOTATE_APP', $null, 'User')
}

Write-Host "AA Annotate uninstalled from:"
Write-Host "  $InstallRoot"
Write-Host "  $skillTarget"
if ($RemoveCodexPlugin) {
    Write-Host "  $pluginTarget"
    Write-Host "  Marketplace entry removed from: $MarketplacePath"
}
