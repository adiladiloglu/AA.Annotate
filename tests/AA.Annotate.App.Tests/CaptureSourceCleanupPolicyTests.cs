using AA.Annotate.App.ViewModels;
using AA.Annotate.Core.Geometry;
using AA.Annotate.Platform;

namespace AA.Annotate.App.Tests;

public sealed class CaptureSourceCleanupPolicyTests
{
    [Fact]
    public void ShouldDeleteRawSourceReturnsFalseForUnmodifiedFullSizeCapture()
    {
        var capture = CreateCapture();

        Assert.False(CaptureSourceCleanupPolicy.ShouldDeleteRawSource(capture, exportScalePercent: 100));
    }

    [Fact]
    public void ShouldDeleteRawSourceReturnsTrueForCroppedCapture()
    {
        var capture = CreateCapture();
        capture.CropPixelRect = new RectInt(10, 10, 80, 80);

        Assert.True(CaptureSourceCleanupPolicy.ShouldDeleteRawSource(capture, exportScalePercent: 100));
    }

    [Fact]
    public void ShouldDeleteRawSourceReturnsTrueForPrivacyMaskedCapture()
    {
        var capture = CreateCapture();
        capture.PrivacyMasks.Add(new PrivacyMaskViewModel("mask", new RectInt(10, 10, 20, 20)));

        Assert.True(CaptureSourceCleanupPolicy.ShouldDeleteRawSource(capture, exportScalePercent: 100));
    }

    [Fact]
    public void ShouldDeleteRawSourceReturnsTrueForDownsizedCapture()
    {
        var capture = CreateCapture();

        Assert.True(CaptureSourceCleanupPolicy.ShouldDeleteRawSource(capture, exportScalePercent: 75));
    }

    private static CaptureViewModel CreateCapture()
    {
        return new CaptureViewModel(
            "capture",
            1,
            new DisplayDescriptor("display", "display", new RectInt(0, 0, 100, 100), IsPrimary: true),
            "screen.png",
            "thumb.png",
            new SizeInt(100, 100),
            new RectInt(0, 0, 100, 100),
            isSelected: true);
    }
}
