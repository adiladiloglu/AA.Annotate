using AA.Annotate.App;

namespace AA.Annotate.App.Tests;

public sealed class AppArgumentTests
{
    [Fact]
    public void ReadSessionRootReturnsFolder()
    {
        var root = App.ReadSessionRoot(["--session-root", "D:\\Annotations"]);

        Assert.Equal("D:\\Annotations", root);
    }

    [Fact]
    public void ReadExportFolderReturnsFolder()
    {
        var output = App.ReadExportFolder(["--session", "C:\\Temp\\session", "--export", "D:\\Exports"]);

        Assert.Equal("D:\\Exports", output);
    }

    [Fact]
    public void ReadIdleTimeoutReturnsSeconds()
    {
        var timeout = App.ReadIdleTimeout(["--session", "C:\\Temp\\session", "--idle-timeout-seconds", "60"]);

        Assert.Equal(TimeSpan.FromSeconds(60), timeout);
    }

    [Fact]
    public void ReadIdleTimeoutUsesOneMinuteDefaultWhenMissing()
    {
        Assert.Equal(TimeSpan.FromMinutes(1), App.ReadIdleTimeout(["--session", "C:\\Temp\\session"]));
    }

    [Fact]
    public void ReadIdleTimeoutIgnoresInvalidValues()
    {
        Assert.Null(App.ReadIdleTimeout(["--idle-timeout-seconds", "0"]));
        Assert.Null(App.ReadIdleTimeout(["--idle-timeout-seconds", "abc"]));
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    [InlineData("/?")]
    public void IsHelpRequestedRecognizesHelpArgs(string arg)
    {
        Assert.True(AppCommandLine.IsHelpRequested([arg]));
    }

    [Fact]
    public void HelpTextIncludesSessionRoot()
    {
        Assert.Contains("--session-root <folder>", AppCommandLine.HelpText, StringComparison.Ordinal);
        Assert.Contains("--export <folder>", AppCommandLine.HelpText, StringComparison.Ordinal);
        Assert.Contains("--idle-timeout-seconds <seconds>", AppCommandLine.HelpText, StringComparison.Ordinal);
        Assert.Contains("Default: 60", AppCommandLine.HelpText, StringComparison.Ordinal);
    }
}
