using AA.Annotate.Core.Models;
using AA.Annotate.Core.Services;
using System.Diagnostics;
using System.Globalization;

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

        try
        {
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
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            TryTerminate(launchedProcess);
            await _store.MarkErrorAsync(paths, $"Annotation session timed out after {FormatTimeout(timeout)}.", cancellationToken);
            return await _store.ReadStatusAsync(paths, cancellationToken);
        }
    }

    private static void TryTerminate(Process? launchedProcess)
    {
        if (launchedProcess is null)
        {
            return;
        }

        try
        {
            if (!launchedProcess.HasExited)
            {
                launchedProcess.Kill(entireProcessTree: true);
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
        }
    }

    private static string FormatTimeout(TimeSpan timeout)
    {
        return timeout.TotalSeconds < 60
            ? $"{timeout.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture)} seconds"
            : timeout.ToString("c");
    }
}
