using AA.Annotate.Core.Models;
using AA.Annotate.Core.Services;
using System.Diagnostics;

namespace AA.Annotate.Cli;

public sealed class SessionWaiter
{
    private readonly SessionStore _store;
    private readonly TimeSpan _pollInterval;

    public SessionWaiter(SessionStore store, TimeSpan? pollInterval = null)
    {
        _store = store;
        _pollInterval = pollInterval ?? TimeSpan.FromMilliseconds(250);
    }

    public async Task<SessionStatusDocument> WaitAsync(
        SessionPaths paths,
        TimeSpan timeout,
        Process? launchedProcess = null,
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        while (true)
        {
            linkedCts.Token.ThrowIfCancellationRequested();
            var status = await _store.ReadStatusAsync(paths, linkedCts.Token);
            if (status.Status is SessionStatus.Completed or SessionStatus.Cancelled or SessionStatus.Error)
            {
                return status;
            }

            if (launchedProcess is { HasExited: true })
            {
                await _store.MarkErrorAsync(paths, $"Annotation app exited before completing the session. Exit code: {launchedProcess.ExitCode}.", cancellationToken);
                return await _store.ReadStatusAsync(paths, cancellationToken);
            }

            await Task.Delay(_pollInterval, linkedCts.Token);
        }
    }
}
