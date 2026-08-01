using AA.Annotate.App.ViewModels;

namespace AA.Annotate.App.Tests;

public sealed class CaptureQualitySelectorPolicyTests
{
    [Fact]
    public void ActiveCaptureShowsQualitySelector()
    {
        var presentation = CreatePresentation(captureSurfaceVisible: true);

        Assert.True(CaptureQualitySelectorPolicy.ShouldShow(
            hasCurrentCapture: true,
            presentation));
    }

    [Fact]
    public void PassiveModeHidesQualitySelectorEvenWhenCaptureExists()
    {
        var presentation = CreatePresentation(captureSurfaceVisible: false);

        Assert.False(CaptureQualitySelectorPolicy.ShouldShow(
            hasCurrentCapture: true,
            presentation));
    }

    [Fact]
    public void MissingCaptureHidesQualitySelectorInActiveMode()
    {
        var presentation = CreatePresentation(captureSurfaceVisible: true);

        Assert.False(CaptureQualitySelectorPolicy.ShouldShow(
            hasCurrentCapture: false,
            presentation));
    }

    private static OverlayPresentation CreatePresentation(bool captureSurfaceVisible)
    {
        return new OverlayPresentation(
            OverlayVisible: captureSurfaceVisible,
            CaptureSurfaceVisible: captureSurfaceVisible,
            ToolbarVisible: true,
            ToolbarEnabled: true);
    }
}
