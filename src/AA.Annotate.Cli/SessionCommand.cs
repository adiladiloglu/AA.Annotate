using AA.Annotate.Core.Models;
using AA.Annotate.Core.Services;

namespace AA.Annotate.Cli;

public sealed class SessionCommand
{
    private readonly TextWriter _output;
    private readonly SessionStore _store;
    private readonly AppLauncher _launcher;
    private readonly SessionWaiter _waiter;

    public SessionCommand(TextWriter output, SessionStore? store = null, AppLauncher? launcher = null, SessionWaiter? waiter = null)
    {
        _output = output;
        _store = store ?? new SessionStore();
        _launcher = launcher ?? new AppLauncher();
        _waiter = waiter ?? new SessionWaiter(_store);
    }

    public async Task<int> RunAsync(IReadOnlyList<string> args, CancellationToken cancellationToken = default)
    {
        if (args.Count == 0 || args[0] != "session")
        {
            await _output.WriteLineAsync("Usage: aa-annotate session --wait [--output <folder>]");
            return 2;
        }

        var wait = args.Contains("--wait", StringComparer.OrdinalIgnoreCase);
        var outputFolder = ReadOption(args, "--output");
        var paths = await _store.CreateSessionAsync(outputFolder, cancellationToken);
        await _output.WriteLineAsync($"SESSION_FOLDER={paths.SessionFolder}");
        await _output.WriteLineAsync($"SESSION_JSON={paths.StatusJsonPath}");

        var launchedProcess = _launcher.Launch(paths.SessionFolder);

        if (!wait)
        {
            return 0;
        }

        var status = await _waiter.WaitAsync(paths, TimeSpan.FromHours(8), launchedProcess, cancellationToken);
        await _output.WriteLineAsync($"SESSION_STATUS={status.Status.ToString().ToLowerInvariant()}");
        if (status.Status == SessionStatus.Completed)
        {
            await _output.WriteLineAsync($"REVIEW_MD={Path.Combine(paths.SessionFolder, status.ReviewPath ?? "review.md")}");
            await _output.WriteLineAsync($"ANNOTATIONS_JSON={Path.Combine(paths.SessionFolder, status.AnnotationsPath ?? "annotations.json")}");
        }

        return status.Status == SessionStatus.Completed ? 0 : 1;
    }

    private static string? ReadOption(IReadOnlyList<string> args, string name)
    {
        for (var i = 0; i < args.Count - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
