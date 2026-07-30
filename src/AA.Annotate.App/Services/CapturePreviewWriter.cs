using AA.Annotate.Core.Geometry;
using AA.Annotate.Core.Models;

namespace AA.Annotate.App.Services;

internal sealed class CapturePreviewWriter
{
    public CapturePreview Write(string originalScreenshotPath, string previewPath, int scalePercent)
    {
        var resolvedScale = CaptureScale.Clamp(scalePercent);
        using var original = PortableImageOperations.Load(originalScreenshotPath);
        var size = new SizeInt(
            Math.Max(1, (int)Math.Round(original.Width * resolvedScale / 100d)),
            Math.Max(1, (int)Math.Round(original.Height * resolvedScale / 100d)));

        if (resolvedScale == CaptureScale.MaximumPercent)
        {
            return new CapturePreview(originalScreenshotPath, size);
        }

        using var preview = PortableImageOperations.Resize(original, size.Width, size.Height);
        PortableImageOperations.SavePng(preview, previewPath);
        return new CapturePreview(previewPath, size);
    }
}

internal sealed record CapturePreview(string ImagePath, SizeInt PixelSize);
