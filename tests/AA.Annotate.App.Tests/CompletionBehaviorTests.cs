using AA.Annotate.App.Services;

namespace AA.Annotate.App.Tests;

public sealed class CompletionBehaviorTests
{
    [Fact]
    public void AgentLaunchPreservesAutomaticHandoff()
    {
        Assert.False(CompletionBehavior.RequiresExportDestination(LaunchCaller.Agent));
    }

    [Fact]
    public void HumanLaunchRequiresExportDestination()
    {
        Assert.True(CompletionBehavior.RequiresExportDestination(LaunchCaller.Human));
    }
}
