using System.Diagnostics;
using AA.Annotate.Cli;
using AA.Annotate.Core.Models;
using AA.Annotate.Core.Services;

namespace AA.Annotate.Cli.Tests;

public sealed class SessionCommandTests
{
    [Fact]
    public async Task RunPrintsHelp()
    {
        var output = new StringWriter();
        var command = new SessionCommand(output);

        var exitCode = await command.RunAsync(["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Usage: aa-annotate session", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("--session-root <folder>", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("--timeout-seconds <seconds>", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunCreatesSessionUnderSessionRoot()
    {
        var output = new StringWriter();
        var store = new SessionStore(() => DateTimeOffset.Parse("2026-06-29T15:30:00Z"));
        var launcher = new RecordingLauncher();
        var command = new SessionCommand(output, store, launcher);
        var root = Path.Combine(Path.GetTempPath(), "AA.Annotate.Cli.Tests", Guid.NewGuid().ToString("N"));

        var exitCode = await command.RunAsync(["session", "--session-root", root]);

        Assert.Equal(0, exitCode);
        Assert.StartsWith(root, launcher.SessionFolder, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"SESSION_FOLDER={launcher.SessionFolder}", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunMarksSessionErrorWhenAppLaunchFails()
    {
        var output = new StringWriter();
        var store = new SessionStore(() => DateTimeOffset.Parse("2026-06-29T15:30:00Z"));
        var command = new SessionCommand(
            output,
            store,
            new ThrowingLauncher("simulated launch failure"));
        var root = Path.Combine(Path.GetTempPath(), "AA.Annotate.Cli.Tests", Guid.NewGuid().ToString("N"));

        var exitCode = await command.RunAsync(["session", "--wait", "--output", root]);

        Assert.Equal(1, exitCode);
        var sessionFolder = Directory.GetDirectories(root).Single();
        var status = await store.ReadStatusAsync(SessionPaths.FromFolder(sessionFolder));
        Assert.Equal(SessionStatus.Error, status.Status);
        Assert.Equal("simulated launch failure", status.ErrorMessage);
        Assert.Contains("SESSION_STATUS=error", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("ERROR_MESSAGE=simulated launch failure", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunPassesTimeoutToAppLauncher()
    {
        var output = new StringWriter();
        var store = new SessionStore(() => DateTimeOffset.Parse("2026-06-29T15:30:00Z"));
        var launcher = new RecordingLauncher();
        var command = new SessionCommand(output, store, launcher);
        var root = Path.Combine(Path.GetTempPath(), "AA.Annotate.Cli.Tests", Guid.NewGuid().ToString("N"));

        var exitCode = await command.RunAsync(["session", "--output", root, "--timeout-seconds", "60"]);

        Assert.Equal(0, exitCode);
        Assert.Equal(TimeSpan.FromSeconds(60), launcher.IdleTimeout);
    }

    [Fact]
    public async Task RunUsesTenMinuteDefaultTimeoutWhenOptionIsOmitted()
    {
        var output = new StringWriter();
        var store = new SessionStore(() => DateTimeOffset.Parse("2026-06-29T15:30:00Z"));
        var launcher = new RecordingLauncher();
        var command = new SessionCommand(output, store, launcher);
        var root = Path.Combine(Path.GetTempPath(), "AA.Annotate.Cli.Tests", Guid.NewGuid().ToString("N"));

        var exitCode = await command.RunAsync(["session", "--output", root]);

        Assert.Equal(0, exitCode);
        Assert.Equal(TimeSpan.FromMinutes(10), launcher.IdleTimeout);
    }

    private sealed class ThrowingLauncher(string message) : AppLauncher
    {
        public override string ResolveExecutablePath()
        {
            return @"C:\Missing\AA.Annotate.App.exe";
        }

        public override Process Launch(string sessionFolder, TimeSpan? idleTimeout = null)
        {
            throw new FileNotFoundException(message);
        }
    }

    private sealed class RecordingLauncher : AppLauncher
    {
        public TimeSpan? IdleTimeout { get; private set; }
        public string? SessionFolder { get; private set; }

        public override string ResolveExecutablePath()
        {
            return @"C:\Tools\AA.Annotate.App.exe";
        }

        public override Process Launch(string sessionFolder, TimeSpan? idleTimeout = null)
        {
            IdleTimeout = idleTimeout;
            SessionFolder = sessionFolder;
            return Process.GetCurrentProcess();
        }
    }
}
