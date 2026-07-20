using AA.Annotate.Cli;
using System.Runtime.InteropServices;

namespace AA.Annotate.Cli.Tests;

public sealed class AppLauncherTests
{
    [Fact]
    public void ResolveExecutablePrefersEnvironmentOverride()
    {
        var root = Path.Combine(Path.GetTempPath(), "AA.Annotate.Cli.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var appPath = Path.Combine(root, "AA.Annotate.App.exe");
        File.WriteAllText(appPath, string.Empty);

        var resolved = AppLauncher.ResolveExecutable(
            AppContext.BaseDirectory,
            name => name == "AA_ANNOTATE_APP" ? appPath : null);

        Assert.Equal(appPath, resolved);
    }

    [Fact]
    public void ResolveExecutableFindsSiblingPublishedApp()
    {
        var root = Path.Combine(Path.GetTempPath(), "AA.Annotate.Cli.Tests", Guid.NewGuid().ToString("N"));
        var cliFolder = Path.Combine(root, "cli");
        var appFolder = Path.Combine(root, "app");
        Directory.CreateDirectory(cliFolder);
        Directory.CreateDirectory(appFolder);
        var appPath = Path.Combine(appFolder, "AA.Annotate.App.exe");
        File.WriteAllText(appPath, string.Empty);

        var resolved = AppLauncher.ResolveExecutable(cliFolder, _ => null);

        Assert.Equal(appPath, resolved);
    }

    [Fact]
    public void ResolveExecutableFindsRepoLocalRuntimePublishedApp()
    {
        var root = Path.Combine(Path.GetTempPath(), "AA.Annotate.Cli.Tests", Guid.NewGuid().ToString("N"));
        var publishFolder = Path.Combine(root, "artifacts", "publish");
        var cliFolder = Path.Combine(publishFolder, "cli-win-x64");
        var appFolder = Path.Combine(publishFolder, "app-win-x64");
        Directory.CreateDirectory(cliFolder);
        Directory.CreateDirectory(appFolder);
        var appPath = Path.Combine(appFolder, "AA.Annotate.App.exe");
        File.WriteAllText(appPath, string.Empty);

        var resolved = AppLauncher.ResolveExecutable(cliFolder, _ => null);

        Assert.Equal(appPath, resolved);
    }

    [Fact]
    public void ResolveExecutableFindsLinuxSiblingPublishedApp()
    {
        var root = CreateTempDirectory();
        var cliFolder = Path.Combine(root, "cli");
        var appFolder = Path.Combine(root, "app");
        Directory.CreateDirectory(cliFolder);
        Directory.CreateDirectory(appFolder);
        var appPath = Path.Combine(appFolder, "AA.Annotate.App");
        File.WriteAllText(appPath, string.Empty);

        var resolved = AppLauncher.ResolveExecutable(
            cliFolder,
            _ => null,
            AppHostPlatform.Linux,
            Architecture.X64);

        Assert.Equal(appPath, resolved);
    }

    [Fact]
    public void ResolveExecutableFindsLinuxRuntimePublishedAppForCurrentArchitecture()
    {
        var root = CreateTempDirectory();
        var publishFolder = Path.Combine(root, "artifacts", "publish");
        var cliFolder = Path.Combine(publishFolder, "cli-linux-arm64");
        var appFolder = Path.Combine(publishFolder, "app-linux-arm64");
        Directory.CreateDirectory(cliFolder);
        Directory.CreateDirectory(appFolder);
        var appPath = Path.Combine(appFolder, "AA.Annotate.App");
        File.WriteAllText(appPath, string.Empty);

        var resolved = AppLauncher.ResolveExecutable(
            cliFolder,
            _ => null,
            AppHostPlatform.Linux,
            Architecture.Arm64);

        Assert.Equal(appPath, resolved);
    }

    [Fact]
    public void ResolveExecutableFindsMacAppBundleInSiblingPackage()
    {
        var root = CreateTempDirectory();
        var cliFolder = Path.Combine(root, "cli");
        var macOsFolder = Path.Combine(root, "app", "AA.Annotate.App.app", "Contents", "MacOS");
        Directory.CreateDirectory(cliFolder);
        Directory.CreateDirectory(macOsFolder);
        var appPath = Path.Combine(macOsFolder, "AA.Annotate.App");
        File.WriteAllText(appPath, string.Empty);

        var resolved = AppLauncher.ResolveExecutable(
            cliFolder,
            _ => null,
            AppHostPlatform.MacOS,
            Architecture.Arm64);

        Assert.Equal(appPath, resolved);
    }

    [Fact]
    public void ResolveExecutableFindsMacRuntimePublishedApp()
    {
        var root = CreateTempDirectory();
        var publishFolder = Path.Combine(root, "artifacts", "publish");
        var cliFolder = Path.Combine(publishFolder, "cli-osx-x64");
        var macOsFolder = Path.Combine(
            publishFolder,
            "app-osx-x64",
            "AA.Annotate.App.app",
            "Contents",
            "MacOS");
        Directory.CreateDirectory(cliFolder);
        Directory.CreateDirectory(macOsFolder);
        var appPath = Path.Combine(macOsFolder, "AA.Annotate.App");
        File.WriteAllText(appPath, string.Empty);

        var resolved = AppLauncher.ResolveExecutable(
            cliFolder,
            _ => null,
            AppHostPlatform.MacOS,
            Architecture.X64);

        Assert.Equal(appPath, resolved);
    }

    [Fact]
    public void ResolveExecutableAcceptsMacAppBundleEnvironmentOverride()
    {
        var root = CreateTempDirectory();
        var bundle = Path.Combine(root, "AA.Annotate.App.app");
        var macOsFolder = Path.Combine(bundle, "Contents", "MacOS");
        Directory.CreateDirectory(macOsFolder);
        var appPath = Path.Combine(macOsFolder, "AA.Annotate.App");
        File.WriteAllText(appPath, string.Empty);

        var resolved = AppLauncher.ResolveExecutable(
            root,
            name => name == "AA_ANNOTATE_APP" ? bundle : null,
            AppHostPlatform.MacOS,
            Architecture.Arm64);

        Assert.Equal(appPath, resolved);
    }

    [Fact]
    public void ResolveExecutableAcceptsMacInnerExecutableEnvironmentOverride()
    {
        var root = CreateTempDirectory();
        var appPath = Path.Combine(root, "AA.Annotate.App");
        File.WriteAllText(appPath, string.Empty);

        var resolved = AppLauncher.ResolveExecutable(
            root,
            name => name == "AA_ANNOTATE_APP" ? appPath : null,
            AppHostPlatform.MacOS,
            Architecture.Arm64);

        Assert.Equal(appPath, resolved);
    }

    [Fact]
    public void ResolveExecutableReportsMissingEnvironmentOverride()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), "AA.Annotate.Cli.Tests", Guid.NewGuid().ToString("N"), "missing.exe");

        var exception = Assert.Throws<FileNotFoundException>(() =>
            AppLauncher.ResolveExecutable(AppContext.BaseDirectory, name => name == "AA_ANNOTATE_APP" ? missingPath : null));

        Assert.Contains("AA_ANNOTATE_APP", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateStartInfoIncludesIdleTimeoutWhenProvided()
    {
        var startInfo = AppLauncher.CreateStartInfo(
            @"C:\Tools\AA.Annotate.App.exe",
            @"C:\Temp\AA.Annotate\sessions\1",
            @"C:\Temp\AA.Annotate\exports\1",
            TimeSpan.FromSeconds(60));

        Assert.Equal(@"C:\Tools\AA.Annotate.App.exe", startInfo.FileName);
        Assert.True(startInfo.UseShellExecute);
        Assert.Equal(
            ["--session", @"C:\Temp\AA.Annotate\sessions\1", "--export", @"C:\Temp\AA.Annotate\exports\1", "--idle-timeout-seconds", "60"],
            startInfo.ArgumentList);
    }

    [Fact]
    public void CreateStartInfoDirectlyExecutesLinuxAppHost()
    {
        AssertUnixStartInfo(AppHostPlatform.Linux);
    }

    [Fact]
    public void CreateStartInfoDirectlyExecutesMacAppHost()
    {
        AssertUnixStartInfo(AppHostPlatform.MacOS);
    }

    [Fact]
    public void FindNewestBuildExecutableDoesNotSelectAnotherUnixRid()
    {
        var root = CreateTempDirectory();
        var linuxFolder = Path.Combine(root, "net10.0", "linux-x64");
        var macFolder = Path.Combine(root, "net10.0", "osx-x64");
        Directory.CreateDirectory(linuxFolder);
        Directory.CreateDirectory(macFolder);
        var linuxPath = Path.Combine(linuxFolder, "AA.Annotate.App");
        var macPath = Path.Combine(macFolder, "AA.Annotate.App");
        File.WriteAllText(linuxPath, string.Empty);
        File.WriteAllText(macPath, string.Empty);
        File.SetLastWriteTimeUtc(macPath, DateTime.UtcNow.AddMinutes(1));

        var resolved = AppLauncher.FindNewestBuildExecutable(
            root,
            AppHostPlatform.Linux,
            Architecture.X64);

        Assert.Equal(linuxPath, resolved);
    }

    private static void AssertUnixStartInfo(AppHostPlatform platform)
    {
        var startInfo = AppLauncher.CreateStartInfo(
            "/opt/aa-annotate/AA.Annotate.App",
            "/tmp/aa annotate/session",
            "/tmp/aa annotate/export",
            null,
            platform);

        Assert.Equal("/opt/aa-annotate/AA.Annotate.App", startInfo.FileName);
        Assert.False(startInfo.UseShellExecute);
        Assert.Equal(
            ["--session", "/tmp/aa annotate/session", "--export", "/tmp/aa annotate/export"],
            startInfo.ArgumentList);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "AA.Annotate.Cli.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
