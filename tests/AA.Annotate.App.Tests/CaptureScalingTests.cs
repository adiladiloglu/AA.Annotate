using AA.Annotate.App.ViewModels;
using AA.Annotate.Core.Geometry;
using AA.Annotate.Platform;

namespace AA.Annotate.App.Tests;

public sealed class CaptureScalingTests
{
    [Fact]
    public void CapturesRetainIndependentScaleSelections()
    {
        var first = CreateCapture("first", 1);
        var second = CreateCapture("second", 2);

        first.SetScalePreview(50, "first-preview.png", new SizeInt(600, 400));
        second.SetScalePreview(75, "second-preview.png", new SizeInt(900, 600));

        Assert.Equal(50, first.ExportScalePercent);
        Assert.Equal(new SizeInt(600, 400), first.PreviewPixelSize);
        Assert.Equal(75, second.ExportScalePercent);
        Assert.Equal(new SizeInt(900, 600), second.PreviewPixelSize);
    }

    [Fact]
    public void ChangingScaleDoesNotChangeOriginalGeometry()
    {
        var capture = CreateCapture("capture", 1);
        var crop = new RectInt(100, 80, 900, 600);
        var annotationRect = new RectInt(140, 120, 240, 160);
        var maskRect = new RectInt(700, 400, 180, 120);
        capture.CropPixelRect = crop;
        capture.Annotations.Add(new AnnotationViewModel("annotation", 1, annotationRect, "button"));
        capture.PrivacyMasks.Add(new PrivacyMaskViewModel("mask", maskRect));

        capture.SetScalePreview(50, "preview.png", new SizeInt(600, 400));

        Assert.Equal(new SizeInt(1200, 800), capture.ScreenshotPixelSize);
        Assert.Equal(crop, capture.CropPixelRect);
        Assert.Equal(annotationRect, capture.Annotations.Single().BoxRect);
        Assert.Equal(maskRect, capture.PrivacyMasks.Single().BoxRect);
    }

    private static CaptureViewModel CreateCapture(string id, int number)
    {
        return new CaptureViewModel(
            id,
            number,
            new DisplayDescriptor("display", "display", new RectInt(0, 0, 1200, 800), IsPrimary: true),
            $"{id}-screen.png",
            $"{id}-thumb.png",
            new SizeInt(1200, 800),
            new RectInt(0, 0, 1200, 800),
            isSelected: number == 1);
    }
}
