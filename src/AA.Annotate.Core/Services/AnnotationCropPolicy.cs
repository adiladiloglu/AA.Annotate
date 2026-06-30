using AA.Annotate.Core.Geometry;

namespace AA.Annotate.Core.Services;

public enum AnnotationCropExportState
{
    Included,
    Clipped,
    Excluded
}

public readonly record struct AnnotationCropExportResult(
    AnnotationCropExportState State,
    RectInt? ExportBoxRect);

public static class AnnotationCropPolicy
{
    public const int MinimumExportBoxSize = 12;

    public static AnnotationCropExportResult Classify(RectInt annotationBox, RectInt crop, int minimumExportSize = MinimumExportBoxSize)
    {
        var left = Math.Max(annotationBox.X, crop.X);
        var top = Math.Max(annotationBox.Y, crop.Y);
        var right = Math.Min(annotationBox.Right, crop.Right);
        var bottom = Math.Min(annotationBox.Bottom, crop.Bottom);
        var width = right - left;
        var height = bottom - top;

        if (width < minimumExportSize || height < minimumExportSize)
        {
            return new AnnotationCropExportResult(AnnotationCropExportState.Excluded, null);
        }

        var clipped = left != annotationBox.X ||
            top != annotationBox.Y ||
            right != annotationBox.Right ||
            bottom != annotationBox.Bottom;
        var exportBox = new RectInt(left - crop.X, top - crop.Y, width, height);
        return new AnnotationCropExportResult(
            clipped ? AnnotationCropExportState.Clipped : AnnotationCropExportState.Included,
            exportBox);
    }
}
