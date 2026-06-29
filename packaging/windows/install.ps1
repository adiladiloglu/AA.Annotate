param(
    [string]$InstallRoot = (Join-Path $env:LOCALAPPDATA 'AA.Annotate'),
    [string]$SkillsRoot = (Join-Path $env:USERPROFILE '.codex\skills'),
    [switch]$AddCliToUserPath,
    [switch]$SetUserAppEnvironmentVariable
)

$ErrorActionPreference = 'Stop'

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

Write-Host "AA Annotate installed."
Write-Host "App:   $appExe"
Write-Host "CLI:   $cliExe"
Write-Host "Skill: $skillTarget"
Write-Host ""
Write-Host "Run without PATH changes:"
Write-Host "  & `"$cliExe`" session --wait"
Write-Host ""
Write-Host "Optional user-scoped registration:"
Write-Host "  .\install.ps1 -AddCliToUserPath"
Write-Host "  .\install.ps1 -SetUserAppEnvironmentVariable"
