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
        Assert.Contains("Default: 60", output.ToString(), StringComparison.Ordinal);
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
        Assert.DoesNotContain("SESSION_FOLDER=", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunKeepsOutputSeparateFromPrivateSessionRoot()
    {
        var output = new StringWriter();
        var store = new SessionStore(() => DateTimeOffset.Parse("2026-06-29T15:30:00Z"));
        var launcher = new RecordingLauncher();
        var command = new SessionCommand(output, store, launcher);
        var sessionRoot = Path.Combine(Path.GetTempPath(), "AA.Annotate.Cli.Tests", Guid.NewGuid().ToString("N"), "work");
        var outputRoot = Path.Combine(Path.GetTempPath(), "AA.Annotate.Cli.Tests", Guid.NewGuid().ToString("N"), "export");

        var exitCode = await command.RunAsync(["session", "--session-root", sessionRoot, "--output", outputRoot]);

        Assert.Equal(0, exitCode);
        Assert.NotNull(launcher.SessionFolder);
        Assert.NotNull(launcher.ExportFolder);
        Assert.StartsWith(sessionRoot, launcher.SessionFolder, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(outputRoot, launcher.ExportFolder, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(launcher.SessionFolder, launcher.ExportFolder, StringComparer.OrdinalIgnoreCase);
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

        var exitCode = await command.RunAsync(["session", "--wait", "--session-root", root]);

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

        var exitCode = await command.RunAsync(["session", "--session-root", root, "--timeout-seconds", "60"]);

        Assert.Equal(0, exitCode);
        Assert.Equal(TimeSpan.FromSeconds(60), launcher.IdleTimeout);
    }

    [Fact]
    public async Task RunUsesOneMinuteDefaultTimeoutWhenOptionIsOmitted()
    {
        var output = new StringWriter();
        var store = new SessionStore(() => DateTimeOffset.Parse("2026-06-29T15:30:00Z"));
        var launcher = new RecordingLauncher();
        var command = new SessionCommand(output, store, launcher);
        var root = Path.Combine(Path.GetTempPath(), "AA.Annotate.Cli.Tests", Guid.NewGuid().ToString("N"));

        var exitCode = await command.RunAsync(["session", "--session-root", root]);

        Assert.Equal(0, exitCode);
        Assert.Equal(TimeSpan.FromMinutes(1), launcher.IdleTimeout);
    }

    [Fact]
    public async Task RunPassesDefaultScaleToAppLauncher()
    {
        var output = new StringWriter();
        var store = new SessionStore(() => DateTimeOffset.Parse("2026-06-29T15:30:00Z"));
        var launcher = new RecordingLauncher();
        var command = new SessionCommand(output, store, launcher);
        var root = Path.Combine(Path.GetTempPath(), "AA.Annotate.Cli.Tests", Guid.NewGuid().ToString("N"));

        var exitCode = await command.RunAsync(["session", "--session-root", root, "--default-scale", "50"]);

        Assert.Equal(0, exitCode);
        Assert.Equal(50, launcher.DefaultScalePercent);
    }

    private sealed class ThrowingLauncher(string message) : AppLauncher
    {
        public override string ResolveExecutablePath()
        {
            return @"C:\Missing\AA.Annotate.App.exe";
        }

        public override Process Launch(string sessionFolder, string exportFolder, TimeSpan? idleTimeout = null, int defaultScalePercent = 100)
        {
            throw new FileNotFoundException(message);
        }
    }

    private sealed class RecordingLauncher : AppLauncher
    {
        public TimeSpan? IdleTimeout { get; private set; }
        public string? SessionFolder { get; private set; }
        public string? ExportFolder { get; private set; }
        public int DefaultScalePercent { get; private set; }

        public override string ResolveExecutablePath()
        {
            return @"C:\Tools\AA.Annotate.App.exe";
        }

        public override Process Launch(string sessionFolder, string exportFolder, TimeSpan? idleTimeout = null, int defaultScalePercent = 100)
        {
            IdleTimeout = idleTimeout;
            SessionFolder = sessionFolder;
            ExportFolder = exportFolder;
            DefaultScalePercent = defaultScalePercent;
            return Process.GetCurrentProcess();
        }
    }
}
