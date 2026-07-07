using System.Text.Json;
using AA.Annotate.Core.Models;
using AA.Annotate.Core.Serialization;
using AA.Annotate.Core.Services;

namespace AA.Annotate.Core.Tests;

public sealed class SessionStoreTests
{
    [Fact]
    public void FromFolderSeparatesPrivateSessionAndExportFoldersByDefault()
    {
        var sessionFolder = Path.Combine(Path.GetTempPath(), "AA.Annotate.Tests", Guid.NewGuid().ToString("N"));

        var paths = SessionPaths.FromFolder(sessionFolder);

        Assert.Equal(sessionFolder, paths.SessionFolder);
        Assert.NotEqual(paths.SessionFolder, paths.ExportFolder, StringComparer.OrdinalIgnoreCase);
        Assert.StartsWith(paths.SessionFolder, paths.StatusJsonPath, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(paths.ExportFolder, paths.ReviewMarkdownPath, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(paths.ExportFolder, paths.AnnotationsJsonPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateSessionSeparatesPrivateSessionAndExportFolders()
    {
        var sessionRoot = CreateTempDirectory();
        var exportRoot = CreateTempDirectory();
        var store = new SessionStore(() => DateTimeOffset.Parse("2026-06-28T15:55:00Z"));

        var paths = await store.CreateSessionAsync(sessionRoot, exportRoot);

        Assert.StartsWith(sessionRoot, paths.SessionFolder, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(exportRoot, paths.ExportFolder, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(paths.SessionFolder, paths.ExportFolder, StringComparer.OrdinalIgnoreCase);
        Assert.True(File.Exists(paths.StatusJsonPath));
        Assert.True(Directory.Exists(paths.WorkingCapturesFolder));
        Assert.True(Directory.Exists(paths.ExportCapturesFolder));
        Assert.StartsWith(paths.ExportFolder, paths.ReviewMarkdownPath, StringComparison.OrdinalIgnoreCase);

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
