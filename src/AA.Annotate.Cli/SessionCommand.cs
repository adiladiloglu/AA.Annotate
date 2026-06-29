using AA.Annotate.Core.Models;
using AA.Annotate.Core.Services;
using System.Diagnostics;
using System.Globalization;

namespace AA.Annotate.Cli;

public sealed class SessionCommand
{
    private static readonly TimeSpan IdleWarningDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan WaiterSafetyBuffer = TimeSpan.FromSeconds(15);
    private const string HelpText = """
        Usage: aa-annotate session [--wait] [--session-root <folder>] [--output <folder>] [--timeout-seconds <seconds>]

        Other commands:
          aa-annotate --help

        Options:
          --wait                         Wait until the user completes, cancels, or the session errors.
          --session-root <folder>        Store created session folders under this root folder.
          --output <folder>              Alias for --session-root.
          --timeout-seconds <seconds>    Inactivity timeout passed to the desktop app. Default: 600.
          -h, --help, /?                 Show this help.
        """;
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
        if (IsHelpRequested(args))
        {
            await _output.WriteLineAsync(HelpText);
            return 0;
        }

        if (args.Count == 0 || args[0] != "session")
        {
            await _output.WriteLineAsync(HelpText);
            return 2;
        }

        var wait = args.Contains("--wait", StringComparer.OrdinalIgnoreCase);
        var outputFolder = ReadOption(args, "--session-root") ?? ReadOption(args, "--output");
        var timeout = ReadTimeout(args);
        var paths = await _store.CreateSessionAsync(outputFolder, cancellationToken);
        await _output.WriteLineAsync($"SESSION_FOLDER={paths.SessionFolder}");
        await _output.WriteLineAsync($"SESSION_JSON={paths.StatusJsonPath}");

        Process launchedProcess;
        try
        {
            var appExecutable = _launcher.ResolveExecutablePath();
            await _output.WriteLineAsync($"APP_EXE={appExecutable}");
            launchedProcess = _launcher.Launch(paths.SessionFolder, timeout);
            await _output.WriteLineAsync($"APP_PROCESS_ID={launchedProcess.Id}");
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            await _store.MarkErrorAsync(paths, exception.Message, cancellationToken);
            await _output.WriteLineAsync("SESSION_STATUS=error");
            await _output.WriteLineAsync($"ERROR_MESSAGE={exception.Message}");
            return 1;
        }

        if (!wait)
        {
            return 0;
        }

        var status = await _waiter.WaitAsync(paths, timeout + IdleWarningDuration + WaiterSafetyBuffer, launchedProcess, cancellationToken);
        await _output.WriteLineAsync($"SESSION_STATUS={status.Status.ToString().ToLowerInvariant()}");
        if (status.Status == SessionStatus.Completed)
        {
            await _output.WriteLineAsync($"REVIEW_MD={Path.Combine(paths.SessionFolder, status.ReviewPath ?? "review.md")}");
            await _output.WriteLineAsync($"ANNOTATIONS_JSON={Path.Combine(paths.SessionFolder, status.AnnotationsPath ?? "annotations.json")}");
        }
        else if (status.Status == SessionStatus.Error && !string.IsNullOrWhiteSpace(status.ErrorMessage))
        {
            await _output.WriteLineAsync($"ERROR_MESSAGE={status.ErrorMessage}");
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

    private static bool IsHelpRequested(IReadOnlyList<string> args)
    {
        return args.Any(arg =>
            string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(arg, "/?", StringComparison.OrdinalIgnoreCase));
    }

    private static TimeSpan ReadTimeout(IReadOnlyList<string> args)
    {
        var value = ReadOption(args, "--timeout-seconds");
        if (string.IsNullOrWhiteSpace(value))
        {
            return TimeSpan.FromMinutes(10);
        }

        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && seconds > 0
            ? TimeSpan.FromSeconds(seconds)
            : throw new ArgumentException($"Invalid --timeout-seconds value: {value}");
    }
}
