using AA.Annotate.App;

namespace AA.Annotate.App.Tests;

public sealed class AppArgumentTests
{
    [Fact]
    public void ReadIdleTimeoutReturnsSeconds()
    {
        var timeout = App.ReadIdleTimeout(["--session", "C:\\Temp\\session", "--idle-timeout-seconds", "60"]);

        Assert.Equal(TimeSpan.FromSeconds(60), timeout);
    }

    [Fact]
    public void ReadIdleTimeoutIgnoresMissingOrInvalidValues()
    {
        Assert.Null(App.ReadIdleTimeout(["--session", "C:\\Temp\\session"]));
        Assert.Null(App.ReadIdleTimeout(["--idle-timeout-seconds", "0"]));
        Assert.Null(App.ReadIdleTimeout(["--idle-timeout-seconds", "abc"]));
    }
}
