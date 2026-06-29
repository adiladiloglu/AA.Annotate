using AA.Annotate.App.ViewModels;

namespace AA.Annotate.App.Tests;

public sealed class CaptureRemovalPolicyTests
{
    [Theory]
    [InlineData(3, 1, 1)]
    [InlineData(3, 2, 1)]
    [InlineData(1, 0, -1)]
    public void SelectReplacementIndexChoosesNearbyCapture(int countBeforeRemoval, int removedIndex, int expectedIndex)
    {
        var index = CaptureRemovalPolicy.SelectReplacementIndex(countBeforeRemoval, removedIndex);

        Assert.Equal(expectedIndex, index);
    }
}
