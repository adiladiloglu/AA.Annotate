using System.Text.Json;
using AA.Annotate.Core.Models;
using AA.Annotate.Core.Serialization;
using AA.Annotate.Core.Services;

namespace AA.Annotate.Core.Tests;

public sealed class SessionStoreTests
{
    [Fact]
    public async Task CreateSessionUsesOutputOverrideWhenProvided()
    {
        var root = CreateTempDirectory();
        var store = new SessionStore(() => DateTimeOffset.Parse("2026-06-28T15:55:00Z"));

        var paths = await store.CreateSessionAsync(root);

        Assert.StartsWith(root, paths.SessionFolder, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(paths.StatusJsonPath));

        var status = JsonSerializer.Deserialize<SessionStatusDocument>(
            await File.ReadAllTextAsync(paths.StatusJsonPath),
            SessionJsonOptions.Create());

        Assert.Equal(SessionStatus.Waiting, status!.Status);
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

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "AA.Annotate.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
