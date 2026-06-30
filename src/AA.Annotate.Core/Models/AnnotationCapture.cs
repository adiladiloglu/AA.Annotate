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
    string? AnnotatedImagePath = null)
{
    public int GetNextAnnotationNumber()
    {
        return Annotations.Count == 0 ? 1 : Annotations.Max(annotation => annotation.Number) + 1;
    }
}
