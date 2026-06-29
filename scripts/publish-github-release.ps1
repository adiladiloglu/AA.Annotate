param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$Runtime = 'win-x64',
    [string]$Repository = 'adiladiloglu/AA.Annotate',
    [string]$Configuration = 'Release',
    [switch]$Prerelease,
    [switch]$Clobber
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$tag = if ($Version.StartsWith('v', [StringComparison]::OrdinalIgnoreCase)) { $Version } else { "v$Version" }
$cleanVersion = $tag.TrimStart('v')
$assetName = "aa-annotate-$cleanVersion-$Runtime.zip"
$assetPath = Join-Path $repoRoot "artifacts\dist\$assetName"
$notesPath = Join-Path $repoRoot "docs\release-notes\$tag.md"

if (-not (Test-Path -LiteralPath $notesPath)) {
    $notesPath = Join-Path $repoRoot 'docs\release-notes\v0.1.0.md'
}

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw 'GitHub CLI is required. Install gh and authenticate with gh auth login.'
}

gh auth status | Out-Host

& (Join-Path $repoRoot 'scripts\package-win.ps1') `
    -Runtime $Runtime `
    -Configuration $Configuration `
    -Version $cleanVersion

if (-not (Test-Path -LiteralPath $assetPath)) {
    throw "Release asset was not created: $assetPath"
}

$existingRelease = gh release view $tag --repo $Repository --json tagName 2>$null
if ($LASTEXITCODE -eq 0 -and -not $Clobber) {
    throw "Release $tag already exists. Re-run with -Clobber to replace the uploaded asset."
}

if ($existingRelease -and $Clobber) {
    gh release upload $tag $assetPath --repo $Repository --clobber
    Write-Host "Updated release asset: $assetPath"
    exit 0
}

$releaseArgs = @(
    'release', 'create', $tag, $assetPath,
    '--repo', $Repository,
    '--title', "AA Annotate $tag",
    '--notes-file', $notesPath,
    '--target', 'master'
)

if ($Prerelease) {
    $releaseArgs += '--prerelease'
}

gh @releaseArgs
Write-Host "Published release $tag to $Repository"
