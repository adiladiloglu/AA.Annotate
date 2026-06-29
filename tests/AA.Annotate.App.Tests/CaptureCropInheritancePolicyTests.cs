using AA.Annotate.App.ViewModels;
using AA.Annotate.Core.Geometry;
using AA.Annotate.Platform;

namespace AA.Annotate.App.Tests;

public sealed class CaptureCropInheritancePolicyTests
{
    [Fact]
    public void TryCopyCropCopiesCroppedRegionForSameScreen()
    {
        var source = CreateCapture("1", new RectInt(100, 80, 700, 500));
        var target = CreateCapture("2", new RectInt(0, 0, 1000, 800));

        var copied = CaptureCropInheritancePolicy.TryCopyCrop(source, target);

        Assert.True(copied);
        Assert.Equal(source.CropPixelRect, target.CropPixelRect);
    }

    [Fact]
    public void TryCopyCropDoesNotCopyAcrossDifferentScreens()
    {
        var source = CreateCapture("1", new RectInt(100, 80, 700, 500));
        var target = CreateCapture("2", new RectInt(0, 0, 1000, 800), displayId: "other");

        var copied = CaptureCropInheritancePolicy.TryCopyCrop(source, target);

        Assert.False(copied);
        Assert.Equal(new RectInt(0, 0, 1000, 800), target.CropPixelRect);
    }

    private static CaptureViewModel CreateCapture(
        string id,
        RectInt crop,
        string displayId = "display")
    {
        var capture = new CaptureViewModel(
            id,
            1,
            new DisplayDescriptor(displayId, displayId, new RectInt(0, 0, 1000, 800), true),
            $"{id}-screen.png",
            $"{id}-thumb.png",
            new SizeInt(1000, 800),
            new RectInt(0, 0, 1000, 800),
            isSelected: true);
        capture.CropPixelRect = crop;
        return capture;
    }
}
