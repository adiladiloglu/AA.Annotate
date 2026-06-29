using AA.Annotate.Core.Geometry;
using AA.Annotate.Core.Models;

namespace AA.Annotate.Core.Tests;

public sealed class SessionModelTests
{
    [Fact]
    public void RectIntRejectsNegativeSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RectInt(0, 0, -1, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RectInt(0, 0, 10, -1));
    }

    [Fact]
    public void CaptureAssignsNextAnnotationNumberWithoutRenumberingExistingAnnotations()
    {
        var capture = new AnnotationCapture(
            CaptureId: "001",
            Number: 1,
            Display: new CaptureDisplay("DISPLAY1", "Primary", new RectInt(0, 0, 1920, 1080)),
            ScreenshotPath: "captures/001/screenshot.png",
            CroppedPath: null,
            ThumbnailPath: "captures/001/thumbnail.png",
            ScreenshotPixelSize: new SizeInt(1920, 1080),
            ScreenBounds: new RectInt(0, 0, 1920, 1080),
            CropRect: null,
            Annotations:
            [
                new Annotation("a1", 1, new RectInt(10, 10, 100, 50), "first"),
                new Annotation("a3", 3, new RectInt(20, 20, 100, 50), "third")
            ]);

        Assert.Equal(4, capture.GetNextAnnotationNumber());
    }
}
