param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$Repository = 'adiladiloglu/AA.Annotate',
    [string]$Branch = 'master',
    [string]$Remote = 'origin'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$tag = if ($Version.StartsWith('v', [StringComparison]::OrdinalIgnoreCase)) { $Version } else { "v$Version" }
$notesPath = Join-Path $repoRoot "docs\release-notes\$tag.md"

if (-not (Test-Path -LiteralPath $notesPath -PathType Leaf)) {
    throw "Release notes were not found: $notesPath"
}

foreach ($command in @('git', 'gh')) {
    if (-not (Get-Command $command -ErrorAction SilentlyContinue)) {
        throw "$command is required to publish a release."
    }
}

Push-Location $repoRoot
try {
    gh auth status | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw 'GitHub CLI authentication is required. Run gh auth login first.'
    }

    $currentBranch = (git branch --show-current).Trim()
    if ($LASTEXITCODE -ne 0 -or $currentBranch -ne $Branch) {
        throw "Release from the $Branch branch. Current branch: $currentBranch"
    }

    $changes = @(git status --porcelain)
    if ($LASTEXITCODE -ne 0 -or $changes.Count -ne 0) {
        throw 'Commit all release changes before publishing.'
    }

    git rev-parse --verify --quiet "refs/tags/$tag" *> $null
    if ($LASTEXITCODE -eq 0) {
        throw "Local tag $tag already exists."
    }

    $remoteTag = @(git ls-remote --tags $Remote "refs/tags/$tag")
    if ($LASTEXITCODE -ne 0) {
        throw "Could not query $Remote for tag $tag."
    }
    if ($remoteTag.Count -ne 0) {
        throw "Remote tag $tag already exists on $Remote."
    }

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    gh release view $tag --repo $Repository --json tagName *> $null
    $releaseViewExitCode = $LASTEXITCODE
    $ErrorActionPreference = $previousErrorActionPreference
    if ($releaseViewExitCode -eq 0) {
        throw "GitHub release $tag already exists in $Repository."
    }

    git push $Remote $Branch
    if ($LASTEXITCODE -ne 0) {
        throw "Pushing $Branch to $Remote failed."
    }

    git tag -a $tag -m "AA.Annotate $tag"
    if ($LASTEXITCODE -ne 0) {
        throw "Creating annotated tag $tag failed."
    }

    git push $Remote "refs/tags/$tag"
    if ($LASTEXITCODE -ne 0) {
        throw "Pushing tag $tag to $Remote failed."
    }

    Write-Host "Pushed $tag. The GitHub release workflow will test and package Windows and Linux before publishing."
    Write-Host "Monitor: https://github.com/$Repository/actions/workflows/release.yml"
}
finally {
    Pop-Location
}
