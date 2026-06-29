using AA.Annotate.Cli;

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
            TimeSpan.FromSeconds(60));

        Assert.Equal(@"C:\Tools\AA.Annotate.App.exe", startInfo.FileName);
        Assert.True(startInfo.UseShellExecute);
        Assert.Equal(
            ["--session", @"C:\Temp\AA.Annotate\sessions\1", "--idle-timeout-seconds", "60"],
            startInfo.ArgumentList);
    }
}
