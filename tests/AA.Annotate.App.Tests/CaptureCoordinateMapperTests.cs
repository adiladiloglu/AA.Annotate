using AA.Annotate.App.ViewModels;
using AA.Annotate.Core.Geometry;
using AA.Annotate.Platform;

namespace AA.Annotate.App.Tests;

public sealed class CaptureCoordinateMapperTests
{
    [Fact]
    public void FullViewportCropMapsToFullScreenshotEvenAfterWindowBecomesCompact()
    {
        var capture = CreateCapture();
        capture.ViewportSize = new SizeInt(1536, 864);
        var fullViewportCrop = new RectInt(0, 0, 1536, 864);

        var pixelCrop = CaptureCoordinateMapper.ToPixelRect(fullViewportCrop, capture);

        Assert.Equal(new RectInt(0, 0, 1920, 1080), pixelCrop);
    }

    [Fact]
    public void StoredPixelCropProjectsToCurrentViewport()
    {
        var viewCrop = CaptureCoordinateMapper.FromPixelRect(
            new RectInt(320, 160, 960, 540),
            new SizeInt(1920, 1080),
            new SizeInt(1536, 864));

        Assert.Equal(new RectInt(256, 128, 768, 432), viewCrop);
    }

    [Fact]
    public void PixelMappingKeepsBottomEdgeAnnotationAtMinimumMeaningfulSize()
    {
        var pixelRect = CaptureCoordinateMapper.ToPixelRect(
            new RectInt(1724, 1599, 308, 1),
            new SizeInt(2560, 1600),
            new SizeInt(2560, 1600));

        Assert.Equal(new RectInt(1724, 1588, 308, 12), pixelRect);
    }

    private static CaptureViewModel CreateCapture()
    {
        return new CaptureViewModel(
            "capture",
            1,
            new DisplayDescriptor("display", "display", new RectInt(0, 0, 1920, 1080), true),
            "screen.png",
            "thumb.png",
            new SizeInt(1920, 1080),
            new RectInt(0, 0, 1920, 1080),
            isSelected: true);
    }
}
