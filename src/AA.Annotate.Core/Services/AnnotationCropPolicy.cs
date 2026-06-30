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
    public static AnnotationCropExportResult Classify(RectInt annotationBox, RectInt crop)
    {
        var left = Math.Max(annotationBox.X, crop.X);
        var top = Math.Max(annotationBox.Y, crop.Y);
        var right = Math.Min(annotationBox.Right, crop.Right);
        var bottom = Math.Min(annotationBox.Bottom, crop.Bottom);

        if (right <= left || bottom <= top)
        {
            return new AnnotationCropExportResult(AnnotationCropExportState.Excluded, null);
        }

        var clipped = left != annotationBox.X ||
            top != annotationBox.Y ||
            right != annotationBox.Right ||
            bottom != annotationBox.Bottom;
        var exportBox = new RectInt(left - crop.X, top - crop.Y, right - left, bottom - top);
        return new AnnotationCropExportResult(
            clipped ? AnnotationCropExportState.Clipped : AnnotationCropExportState.Included,
            exportBox);
    }
}
