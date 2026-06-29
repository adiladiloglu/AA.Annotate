using AA.Annotate.App.ViewModels;

namespace AA.Annotate.App.Tests;

public sealed class CaptureDependentToolPolicyTests
{
    [Fact]
    public void SelectActionCapturesFirstWhenNoCaptureExists()
    {
        Assert.Equal(CaptureDependentToolAction.CaptureFirst, CaptureDependentToolPolicy.SelectAction(hasCapture: false));
    }

    [Fact]
    public void SelectActionUsesCurrentCaptureWhenCaptureExists()
    {
        Assert.Equal(CaptureDependentToolAction.UseCurrentCapture, CaptureDependentToolPolicy.SelectAction(hasCapture: true));
    }
}
