using AA.Annotate.App.ViewModels;
using AA.Annotate.Core.Geometry;

namespace AA.Annotate.App.Tests;

public sealed class OverlayHitTestPolicyTests
{
    [Fact]
    public void ShouldHandlePointHandlesWholeSurfaceWhenBlockingSurfaceIsActive()
    {
        var shouldHandle = OverlayHitTestPolicy.ShouldHandlePoint(
            handleFullSurface: true,
            interactiveRects: [],
            new PointInt(900, 600));

        Assert.True(shouldHandle);
    }

    [Fact]
    public void ShouldHandlePointHandlesVisibleChromeRects()
    {
        var shouldHandle = OverlayHitTestPolicy.ShouldHandlePoint(
            handleFullSurface: false,
            [new RectInt(20, 20, 120, 40)],
            new PointInt(60, 30));

        Assert.True(shouldHandle);
    }

    [Fact]
    public void ShouldHandlePointPassesThroughOutsideVisibleChromeRects()
    {
        var shouldHandle = OverlayHitTestPolicy.ShouldHandlePoint(
            handleFullSurface: false,
            [new RectInt(20, 20, 120, 40)],
            new PointInt(300, 300));

        Assert.False(shouldHandle);
    }
}
