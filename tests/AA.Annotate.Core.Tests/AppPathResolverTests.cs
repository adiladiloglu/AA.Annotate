using AA.Annotate.Core.Services;

namespace AA.Annotate.Core.Tests;

public sealed class AppPathResolverTests
{
    [Fact]
    public void UnixRuntimeUsesValidXdgRuntimeDirectory()
    {
        var root = CreateTempDirectory();
        var resolver = CreateUnixResolver(
            root,
            variable => variable == "XDG_RUNTIME_DIR" ? root : null,
            _ => true);

        var result = resolver.GetPrivateRuntimeDirectory();

        Assert.Equal(Path.Combine(root, "aa-annotate"), result);
        Assert.True(Directory.Exists(result));
    }

    [Fact]
    public void UnixRuntimeRejectsInvalidXdgRuntimeDirectoryAndUsesPerUserFallback()
    {
        var temp = CreateTempDirectory();
        var xdgRuntime = Path.Combine(temp, "invalid-xdg");
        Directory.CreateDirectory(xdgRuntime);
        var resolver = CreateUnixResolver(
            temp,
            variable => variable == "XDG_RUNTIME_DIR" ? xdgRuntime : null,
            _ => false);

        var result = resolver.GetPrivateRuntimeDirectory();

        Assert.Equal(
            Path.Combine(temp, "aa-annotate-runtime-1234", "aa-annotate"),
            result);
        Assert.True(Directory.Exists(result));
    }

    [Fact]
    public void UnixConfigUsesAbsoluteXdgConfigHome()
    {
        var temp = CreateTempDirectory();
        var configHome = Path.Combine(temp, "xdg-config");
        var resolver = CreateUnixResolver(
            temp,
            variable => variable == "XDG_CONFIG_HOME" ? configHome : null);

        var result = resolver.GetConfigDirectory();

        Assert.Equal(Path.Combine(configHome, "aa-annotate"), result);
    }

    [Fact]
    public void UnixConfigRejectsRelativeXdgConfigHome()
    {
        var temp = CreateTempDirectory();
        var home = Path.Combine(temp, "home");
        var resolver = new AppPathResolver(
            variable => variable == "XDG_CONFIG_HOME" ? "relative/config" : null,
            folder => folder == Environment.SpecialFolder.UserProfile ? home : temp,
            () => temp,
            () => "1234",
            _ => false,
            isUnix: true);

        var result = resolver.GetConfigDirectory();

        Assert.Equal(Path.Combine(home, ".config", "aa-annotate"), result);
    }

    [Fact]
    public void WindowsPathsRetainExistingLocations()
    {
        var temp = CreateTempDirectory();
        var localAppData = Path.Combine(temp, "local");
        var resolver = new AppPathResolver(
            _ => null,
            _ => localAppData,
            () => temp,
            () => "ignored",
            _ => false,
            isUnix: false);

        Assert.Equal(
            Path.Combine(temp, "AA.Annotate"),
            resolver.GetPrivateRuntimeDirectory());
        Assert.Equal(
            Path.Combine(localAppData, "AA.Annotate"),
            resolver.GetConfigDirectory());
    }

    [Fact]
    public async Task UnixPrivateArtifactsHaveOwnerOnlyPermissions()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var temp = CreateTempDirectory();
        var resolver = CreateUnixResolver(temp, _ => null);
        var store = new SessionStore(
            () => DateTimeOffset.Parse("2026-06-28T15:55:00Z"),
            resolver);

        var paths = await store.CreateSessionAsync(
            sessionRoot: null,
            exportRoot: Path.Combine(temp, "exports"));

        Assert.Equal(
            PrivateFileSystem.PrivateDirectoryMode,
            File.GetUnixFileMode(Path.GetDirectoryName(paths.SessionFolder)!));
        Assert.Equal(
            PrivateFileSystem.PrivateDirectoryMode,
            File.GetUnixFileMode(paths.SessionFolder));
        Assert.Equal(
            PrivateFileSystem.PrivateDirectoryMode,
            File.GetUnixFileMode(paths.WorkingCapturesFolder));
        Assert.Equal(
            PrivateFileSystem.PrivateFileMode,
            File.GetUnixFileMode(paths.StatusJsonPath));
    }

    [Fact]
    public async Task DefaultSessionAndExportRootsUsePrivateRuntimeDirectory()
    {
        var temp = CreateTempDirectory();
        var resolver = CreateUnixResolver(temp, _ => null);
        var privateRuntime = resolver.GetPrivateRuntimeDirectory();
        var store = new SessionStore(
            () => DateTimeOffset.Parse("2026-06-28T15:55:00Z"),
            resolver);

        var paths = await store.CreateSessionAsync(
            sessionRoot: null,
            exportRoot: null);

        Assert.StartsWith(
            Path.Combine(privateRuntime, "sessions"),
            paths.SessionFolder,
            StringComparison.Ordinal);
        Assert.StartsWith(
            Path.Combine(privateRuntime, "exports"),
            paths.ExportFolder,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ExplicitExportFolderIsPreserved()
    {
        var temp = CreateTempDirectory();
        var resolver = CreateUnixResolver(temp, _ => null);
        var sessionFolder = Path.Combine(temp, "session");
        var explicitExportFolder = Path.Combine(temp, "shared-export");

        var paths = SessionPaths.FromFolder(
            sessionFolder,
            explicitExportFolder,
            resolver);

        Assert.Equal(explicitExportFolder, paths.ExportFolder);
    }

    private static AppPathResolver CreateUnixResolver(
        string temp,
        Func<string, string?> getEnvironmentVariable,
        Func<string, bool>? isValidRuntimeDirectory = null)
    {
        return new AppPathResolver(
            getEnvironmentVariable,
            _ => Path.Combine(temp, "home"),
            () => temp,
            () => "1234",
            isValidRuntimeDirectory ?? (_ => false),
            isUnix: true);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "AA.Annotate.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
