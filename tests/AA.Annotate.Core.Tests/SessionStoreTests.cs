using System.Text.Json;
using AA.Annotate.Core.Models;
using AA.Annotate.Core.Serialization;
using AA.Annotate.Core.Services;

namespace AA.Annotate.Core.Tests;

public sealed class SessionStoreTests
{
    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    private static StringComparer PathComparer =>
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    [Fact]
    public void FromFolderSeparatesPrivateSessionAndExportFoldersByDefault()
    {
        var sessionFolder = Path.Combine(Path.GetTempPath(), "AA.Annotate.Tests", Guid.NewGuid().ToString("N"));

        var paths = SessionPaths.FromFolder(sessionFolder);

        Assert.Equal(sessionFolder, paths.SessionFolder);
        Assert.NotEqual(paths.SessionFolder, paths.ExportFolder, PathComparer);
        Assert.StartsWith(paths.SessionFolder, paths.StatusJsonPath, PathComparison);
        Assert.StartsWith(paths.ExportFolder, paths.ReviewMarkdownPath, PathComparison);
        Assert.StartsWith(paths.ExportFolder, paths.AnnotationsJsonPath, PathComparison);
    }

    [Fact]
    public void FromFolderUsesPlatformPathCaseRulesWhenAvoidingExportCollision()
    {
        var sessionFolder = Path.Combine(
            Path.GetTempPath(),
            "AA.Annotate",
            "EXPORTS",
            $"path-case-{Guid.NewGuid():N}");

        var paths = SessionPaths.FromFolder(sessionFolder);

        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
        {
            Assert.EndsWith("-export", paths.ExportFolder, StringComparison.Ordinal);
        }
        else
        {
            Assert.False(paths.ExportFolder.EndsWith("-export", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void FromFolderRejectsAnExplicitExportFolderThatMatchesThePrivateSessionFolder()
    {
        var sessionFolder = Path.Combine(Path.GetTempPath(), "AA.Annotate.Tests", Guid.NewGuid().ToString("N"));

        var exception = Assert.Throws<ArgumentException>(
            () => SessionPaths.FromFolder(sessionFolder, sessionFolder));

        Assert.Equal("exportFolder", exception.ParamName);
    }

    [Fact]
    public async Task CreateSessionSeparatesPrivateSessionAndExportFolders()
    {
        var sessionRoot = CreateTempDirectory();
        var exportRoot = CreateTempDirectory();
        var store = new SessionStore(() => DateTimeOffset.Parse("2026-06-28T15:55:00Z"));

        var paths = await store.CreateSessionAsync(sessionRoot, exportRoot);

        Assert.StartsWith(sessionRoot, paths.SessionFolder, PathComparison);
        Assert.StartsWith(exportRoot, paths.ExportFolder, PathComparison);
        Assert.NotEqual(paths.SessionFolder, paths.ExportFolder, PathComparer);
        Assert.True(File.Exists(paths.StatusJsonPath));
        Assert.True(Directory.Exists(paths.WorkingCapturesFolder));
        Assert.True(Directory.Exists(paths.ExportCapturesFolder));
        Assert.StartsWith(paths.ExportFolder, paths.ReviewMarkdownPath, PathComparison);

        var status = JsonSerializer.Deserialize<SessionStatusDocument>(
            await File.ReadAllTextAsync(paths.StatusJsonPath),
            SessionJsonOptions.Create());

        Assert.Equal(SessionStatus.Waiting, status!.Status);
        Assert.Equal(DateTimeOffset.Parse("2026-06-28T15:55:00Z"), status.LastActivityAtUtc);
    }

    [Fact]
    public async Task CompleteWritesArtifactsStatus()
    {
        var root = CreateTempDirectory();
        var store = new SessionStore(() => DateTimeOffset.Parse("2026-06-28T15:55:00Z"));
        var paths = await store.CreateSessionAsync(root);

        await store.MarkCompletedAsync(paths, "review.md", "annotations.json");

        var status = JsonSerializer.Deserialize<SessionStatusDocument>(
            await File.ReadAllTextAsync(paths.StatusJsonPath),
            SessionJsonOptions.Create());

        Assert.Equal(SessionStatus.Completed, status!.Status);
        Assert.Equal("review.md", status.ReviewPath);
        Assert.Equal("annotations.json", status.AnnotationsPath);
    }

    [Fact]
    public async Task MarkErrorWritesTerminalErrorStatus()
    {
        var root = CreateTempDirectory();
        var store = new SessionStore(() => DateTimeOffset.Parse("2026-06-28T15:55:00Z"));
        var paths = await store.CreateSessionAsync(root);

        await store.MarkErrorAsync(paths, "app exited");

        var status = JsonSerializer.Deserialize<SessionStatusDocument>(
            await File.ReadAllTextAsync(paths.StatusJsonPath),
            SessionJsonOptions.Create());

        Assert.Equal(SessionStatus.Error, status!.Status);
        Assert.Equal("app exited", status.ErrorMessage);
    }

    [Fact]
    public async Task TouchActivityUpdatesWaitingSessionActivity()
    {
        var root = CreateTempDirectory();
        var now = DateTimeOffset.Parse("2026-06-28T15:55:00Z");
        var store = new SessionStore(() => now);
        var paths = await store.CreateSessionAsync(root);
        now = now.AddMinutes(3);

        await store.TouchActivityAsync(paths);

        var status = JsonSerializer.Deserialize<SessionStatusDocument>(
            await File.ReadAllTextAsync(paths.StatusJsonPath),
            SessionJsonOptions.Create());

        Assert.Equal(SessionStatus.Waiting, status!.Status);
        Assert.Equal(now, status.LastActivityAtUtc);
    }

    [Fact]
    public async Task ReadStatusAllowsSharedWriterHandle()
    {
        var root = CreateTempDirectory();
        var store = new SessionStore(() => DateTimeOffset.Parse("2026-06-28T15:55:00Z"));
        var paths = await store.CreateSessionAsync(root);
        await using var _ = new FileStream(
            paths.StatusJsonPath,
            FileMode.Open,
            FileAccess.Write,
            FileShare.ReadWrite);

        var status = await store.ReadStatusAsync(paths);

        Assert.Equal(SessionStatus.Waiting, status.Status);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "AA.Annotate.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
