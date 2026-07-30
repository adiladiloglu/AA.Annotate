using AA.Annotate.App.Services;
using AA.Annotate.App.ViewModels;

namespace AA.Annotate.App.Tests;

public sealed class SessionConfirmationPolicyTests
{
    [Fact]
    public void AgentFinishConfirmsSending()
    {
        var presentation = SessionConfirmationPolicy.CreateFinish(LaunchCaller.Agent);

        Assert.Equal("Finish annotation session?", presentation.Title);
        Assert.Equal("Send", presentation.ConfirmText);
        Assert.False(presentation.IsDestructive);
    }

    [Fact]
    public void HumanFinishConfirmsExporting()
    {
        var presentation = SessionConfirmationPolicy.CreateFinish(LaunchCaller.Human);

        Assert.Equal("Export", presentation.ConfirmText);
        Assert.False(presentation.IsDestructive);
    }

    [Fact]
    public void CancelWarnsAboutDiscardingWork()
    {
        var presentation = SessionConfirmationPolicy.CreateCancel();

        Assert.Equal("Cancel annotation session?", presentation.Title);
        Assert.Contains("discard", presentation.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Cancel session", presentation.ConfirmText);
        Assert.True(presentation.IsDestructive);
    }
}
