param(
    [string]$InstallRoot = (Join-Path $env:LOCALAPPDATA 'AA.Annotate'),
    [string]$SkillsRoot = (Join-Path $env:USERPROFILE '.codex\skills'),
    [switch]$RemoveCliFromUserPath,
    [switch]$RemoveUserAppEnvironmentVariable
)

$ErrorActionPreference = 'Stop'

$cliTarget = Join-Path $InstallRoot 'cli'
$skillTarget = Join-Path $SkillsRoot 'aa-annotate'

if (Test-Path -LiteralPath $InstallRoot) {
    Remove-Item -LiteralPath $InstallRoot -Recurse -Force
}

if (Test-Path -LiteralPath $skillTarget) {
    Remove-Item -LiteralPath $skillTarget -Recurse -Force
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
