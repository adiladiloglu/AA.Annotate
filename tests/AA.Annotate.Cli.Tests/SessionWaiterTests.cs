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
        var store = new SessionStore(() => DateTimeOffset.Parse("2026-06-28T15:55:00Z"));
        var paths = await store.CreateSessionAsync(root);
        var waiter = new SessionWaiter(store, TimeSpan.FromMilliseconds(10));

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
        Assert.Equal("Annotation session timed out after 0.05 seconds.", result.ErrorMessage);
    }
}
