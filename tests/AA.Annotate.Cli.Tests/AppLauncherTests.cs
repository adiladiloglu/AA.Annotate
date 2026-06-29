using AA.Annotate.Cli;

namespace AA.Annotate.Cli.Tests;

public sealed class AppLauncherTests
{
    [Fact]
    public void ResolveExecutablePrefersEnvironmentOverride()
    {
        var resolved = AppLauncher.ResolveExecutable(
            AppContext.BaseDirectory,
            name => name == "AA_ANNOTATE_APP" ? @"C:\Tools\AA.Annotate.App.exe" : null);

        Assert.Equal(@"C:\Tools\AA.Annotate.App.exe", resolved);
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
}
