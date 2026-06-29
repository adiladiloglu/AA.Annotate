using AA.Annotate.Cli;
using AA.Annotate.Core.Models;
using AA.Annotate.Core.Services;

namespace AA.Annotate.Cli.Tests;

public sealed class SessionWaiterTests
{
    [Fact]
    public async Task WaitReturnsWhenSessionCompletes()
    {
        var root = Path.Combine(Path.GetTempPath(), "AA.Annotate.Cli.Tests", Guid.NewGuid().ToString("N"));
        var now = DateTimeOffset.Parse("2026-06-28T15:55:00Z");
        var store = new SessionStore(() => now);
        var paths = await store.CreateSessionAsync(root);
        var waiter = new SessionWaiter(store, TimeSpan.FromMilliseconds(10), () => now);

        _ = Task.Run(async () =>
        {
            await Task.Delay(40);
            await store.MarkCompletedAsync(paths, "review.md", "annotations.json");
        });

        var result = await waiter.WaitAsync(paths, TimeSpan.FromSeconds(2));

        Assert.Equal(SessionStatus.Completed, result.Status);
    }

    [Fact]
    public async Task WaitMarksErrorWhenSessionTimesOut()
    {
        var root = Path.Combine(Path.GetTempPath(), "AA.Annotate.Cli.Tests", Guid.NewGuid().ToString("N"));
        var store = new SessionStore(() => DateTimeOffset.Parse("2026-06-28T15:55:00Z"));
        var paths = await store.CreateSessionAsync(root);
        var waiter = new SessionWaiter(store, TimeSpan.FromMilliseconds(10));

        var result = await waiter.WaitAsync(paths, TimeSpan.FromMilliseconds(50));

        Assert.Equal(SessionStatus.Error, result.Status);
        Assert.Equal("Annotation session timed out after 0.05 seconds without user activity.", result.ErrorMessage);
    }

    [Fact]
    public async Task WaitUsesLatestUserActivityAsTimeoutBaseline()
    {
        var root = Path.Combine(Path.GetTempPath(), "AA.Annotate.Cli.Tests", Guid.NewGuid().ToString("N"));
        var now = DateTimeOffset.Parse("2026-06-28T15:55:00Z");
        var store = new SessionStore(() => now);
        var paths = await store.CreateSessionAsync(root);
        now = now.AddMilliseconds(40);
        await store.TouchActivityAsync(paths);
        now = now.AddMilliseconds(30);
        var waiter = new SessionWaiter(store, TimeSpan.FromMilliseconds(5), () => now);

        _ = Task.Run(async () =>
        {
            await Task.Delay(20);
            await store.MarkCompletedAsync(paths, "review.md", "annotations.json");
        });

        var result = await waiter.WaitAsync(paths, TimeSpan.FromMilliseconds(50));

        Assert.Equal(SessionStatus.Completed, result.Status);
    }
}
