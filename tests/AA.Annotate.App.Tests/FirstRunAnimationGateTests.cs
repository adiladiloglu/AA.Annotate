using AA.Annotate.App.ViewModels;

namespace AA.Annotate.App.Tests;

public sealed class FirstRunAnimationGateTests
{
    [Fact]
    public void TryClaimReturnsTrueOnlyOnceForMarker()
    {
        var marker = Path.Combine(Path.GetTempPath(), "AA.Annotate.App.Tests", Guid.NewGuid().ToString("N"), "first-run-animation.seen");

        Assert.True(FirstRunAnimationGate.TryClaim(marker));
        Assert.False(FirstRunAnimationGate.TryClaim(marker));
        Assert.True(File.Exists(marker));
    }
}
