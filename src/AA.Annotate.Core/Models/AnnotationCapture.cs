using AA.Annotate.Core.Geometry;

namespace AA.Annotate.Core.Models;

public sealed record AnnotationCapture(
    string CaptureId,
    int Number,
    CaptureDisplay Display,
    string ScreenshotPath,
    string? CroppedPath,
    string ThumbnailPath,
    SizeInt ScreenshotPixelSize,
    RectInt ScreenBounds,
    RectInt? CropRect,
    IReadOnlyList<Annotation> Annotations,
    string? AnnotatedImagePath = null,
    IReadOnlyList<PrivacyMask>? PrivacyMasks = null,
    int ExportScalePercent = 100)
{
    public int GetNextAnnotationNumber()
    {
        return Annotations.Count == 0 ? 1 : Annotations.Max(annotation => annotation.Number) + 1;
    }
}
