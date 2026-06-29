param(
    [string]$Repository = 'adiladiloglu/AA.Annotate',
    [string]$Version = 'latest',
    [string]$Runtime = 'win-x64',
    [string]$InstallRoot = (Join-Path $env:LOCALAPPDATA 'AA.Annotate'),
    [string]$SkillsRoot = (Join-Path $env:USERPROFILE '.codex\skills'),
    [string]$PluginsRoot = (Join-Path $env:USERPROFILE 'plugins'),
    [string]$MarketplacePath = (Join-Path $env:USERPROFILE '.agents\plugins\marketplace.json'),
    [string]$GitHubToken = $(if ($env:GITHUB_TOKEN) { $env:GITHUB_TOKEN } else { $env:GH_TOKEN }),
    [switch]$AddCliToUserPath,
    [switch]$SetUserAppEnvironmentVariable,
    [switch]$InstallCodexPlugin,
    [switch]$KeepDownload
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http

function New-InstallerHttpClient {
    param(
        [string]$GitHubToken
    )

    $client = [System.Net.Http.HttpClient]::new()
    $client.DefaultRequestHeaders.UserAgent.ParseAdd('AA-Annotate-Installer')
    $client.DefaultRequestHeaders.Accept.ParseAdd('application/vnd.github+json')

    if (-not [string]::IsNullOrWhiteSpace($GitHubToken)) {
        $client.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $GitHubToken)
    }

    $client
}

function Get-JsonFromUri {
    param(
        [System.Net.Http.HttpClient]$Client,
        [string]$Uri
    )

    try {
        $json = $Client.GetStringAsync($Uri).GetAwaiter().GetResult()
        $json | ConvertFrom-Json
    }
    catch [System.Net.Http.HttpRequestException] {
        throw "Failed to read GitHub release metadata from $Uri. Confirm that the release exists and is public. $($_.Exception.Message)"
    }
}

function Save-UriToFile {
    param(
        [System.Net.Http.HttpClient]$Client,
        [string]$Uri,
        [string]$Path
    )

    $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Get, $Uri)
    $request.Headers.Accept.Clear()
    $request.Headers.Accept.ParseAdd('application/octet-stream')

    try {
        $response = $Client.SendAsync($request).GetAwaiter().GetResult()
        $response.EnsureSuccessStatusCode() | Out-Null
        $bytes = $response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
        [System.IO.File]::WriteAllBytes($Path, $bytes)
    }
    catch [System.Net.Http.HttpRequestException] {
        throw "Failed to download AA Annotate release asset from $Uri. $($_.Exception.Message)"
    }
    finally {
        $request.Dispose()
        if ($null -ne $response) {
            $response.Dispose()
        }
    }
}

function Get-GitHubRelease {
    param(
        [System.Net.Http.HttpClient]$Client,
        [string]$Repository,
        [string]$Version
    )

    $releaseUri = if ($Version -eq 'latest') {
        "https://api.github.com/repos/$Repository/releases/latest"
    }
    else {
        $tag = if ($Version.StartsWith('v', [StringComparison]::OrdinalIgnoreCase)) { $Version } else { "v$Version" }
        "https://api.github.com/repos/$Repository/releases/tags/$tag"
    }

    Get-JsonFromUri -Client $Client -Uri $releaseUri
}

function Get-ReleaseAsset {
    param(
        [object]$Release,
        [string]$Runtime
    )

    $assetPattern = "aa-annotate-*-$Runtime.zip"
    $asset = $Release.assets |
        Where-Object { $_.name -like $assetPattern } |
        Select-Object -First 1

    if ($null -eq $asset) {
        $available = ($Release.assets | ForEach-Object { $_.name }) -join ', '
        throw "No release asset matched '$assetPattern'. Available assets: $available"
    }

    $asset
}

function Invoke-PackagedInstaller {
    param(
        [string]$PackageRoot
    )

    $installer = Join-Path $PackageRoot 'install.ps1'
    if (-not (Test-Path -LiteralPath $installer)) {
        throw "Downloaded package is incomplete. Missing: $installer"
    }

    $arguments = @{
        InstallRoot = $InstallRoot
        SkillsRoot = $SkillsRoot
        PluginsRoot = $PluginsRoot
        MarketplacePath = $MarketplacePath
    }

    if ($AddCliToUserPath) {
        $arguments.AddCliToUserPath = $true
    }

    if ($SetUserAppEnvironmentVariable) {
        $arguments.SetUserAppEnvironmentVariable = $true
    }

    if ($InstallCodexPlugin) {
        $arguments.InstallCodexPlugin = $true
    }

    & $installer @arguments
}

function Test-Installation {
    $appExe = Join-Path $InstallRoot 'app\AA.Annotate.App.exe'
    $cliExe = Join-Path $InstallRoot 'cli\aa-annotate.exe'
    $skillFile = Join-Path $SkillsRoot 'aa-annotate\SKILL.md'

    foreach ($requiredPath in @($appExe, $cliExe, $skillFile)) {
        if (-not (Test-Path -LiteralPath $requiredPath)) {
            throw "Install verification failed. Missing: $requiredPath"
        }
    }

    if ($InstallCodexPlugin) {
        $pluginRoot = Join-Path $PluginsRoot 'aa-annotate'
        $pluginCli = Join-Path $pluginRoot 'cli\aa-annotate.exe'
        $pluginManifest = Join-Path $pluginRoot '.codex-plugin\plugin.json'

        foreach ($requiredPath in @($pluginCli, $pluginManifest, $MarketplacePath)) {
            if (-not (Test-Path -LiteralPath $requiredPath)) {
                throw "Codex plugin install verification failed. Missing: $requiredPath"
            }
        }
    }

    Write-Host ''
    Write-Host 'AA Annotate bootstrap verification passed.'
    Write-Host "App:   $appExe"
    Write-Host "CLI:   $cliExe"
    Write-Host "Skill: $skillFile"
    if ($InstallCodexPlugin) {
        Write-Host "Plugin: $(Join-Path $PluginsRoot 'aa-annotate')"
        Write-Host "Marketplace: $MarketplacePath"
    }
}

$client = New-InstallerHttpClient -GitHubToken $GitHubToken
$release = Get-GitHubRelease -Client $client -Repository $Repository -Version $Version
$asset = Get-ReleaseAsset -Release $release -Runtime $Runtime

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('AA.Annotate.Install.' + [Guid]::NewGuid().ToString('N'))
$downloadPath = Join-Path $tempRoot $asset.name
$extractRoot = Join-Path $tempRoot 'extract'

New-Item -ItemType Directory -Path $tempRoot -Force | Out-Null
New-Item -ItemType Directory -Path $extractRoot -Force | Out-Null

try {
    Write-Host "Downloading AA Annotate $($release.tag_name): $($asset.name)"
    Save-UriToFile -Client $client -Uri $asset.url -Path $downloadPath

    Expand-Archive -LiteralPath $downloadPath -DestinationPath $extractRoot -Force
    $packageRoot = if (Test-Path -LiteralPath (Join-Path $extractRoot 'install.ps1')) {
        Get-Item -LiteralPath $extractRoot
    }
    else {
        Get-ChildItem -LiteralPath $extractRoot -Directory |
            Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'install.ps1') } |
            Select-Object -First 1
    }

    if ($null -eq $packageRoot) {
        throw "Downloaded archive did not contain a package folder with install.ps1: $downloadPath"
    }

    Invoke-PackagedInstaller -PackageRoot $packageRoot.FullName
    Test-Installation
}
finally {
    $client.Dispose()

    if (-not $KeepDownload -and (Test-Path -LiteralPath $tempRoot)) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}
