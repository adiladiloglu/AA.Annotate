using System.Diagnostics;
using AA.Annotate.Cli;
using AA.Annotate.Core.Models;
using AA.Annotate.Core.Services;

namespace AA.Annotate.Cli.Tests;

public sealed class SessionCommandTests
{
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

    private sealed class ThrowingLauncher(string message) : AppLauncher
    {
        public override string ResolveExecutablePath()
        {
            return @"C:\Missing\AA.Annotate.App.exe";
        }

        public override Process Launch(string sessionFolder)
        {
            throw new FileNotFoundException(message);
        }
    }
}
