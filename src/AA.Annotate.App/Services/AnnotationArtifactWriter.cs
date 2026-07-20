using AA.Annotate.Core.Geometry;
using AA.Annotate.Core.Models;
using AA.Annotate.Core.Services;
using SkiaSharp;

namespace AA.Annotate.App.Services;

public sealed class AnnotationArtifactWriter : IAnnotationArtifactWriter
{
    public Task<AnnotationCapture> WriteAsync(
        SessionPaths paths,
        AnnotationCapture capture,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(paths.CapturesFolder);
        using var source = PortableImageOperations.Load(capture.ScreenshotPath);
        using var exportedSource = CreateExportedSource(source, capture);
        var annotations = ScaleAnnotations(capture.Annotations, capture.ExportScalePercent);
        var privacyMasks = ScalePrivacyMasks(capture.PrivacyMasks, capture.ExportScalePercent);
        var primaryPath = Path.Combine(paths.CapturesFolder, $"{capture.Number:00}-export.png");
        PortableImageOperations.SavePng(exportedSource, primaryPath);
        var croppedPath = string.IsNullOrWhiteSpace(capture.CroppedPath) ? null : primaryPath;
        var thumbnailPath = primaryPath;

        if (annotations.Count == 0)
        {
            return Task.FromResult(capture with
            {
                ScreenshotPath = primaryPath,
                CroppedPath = croppedPath,
                ThumbnailPath = thumbnailPath,
                PrivacyMasks = privacyMasks,
                ExportScalePercent = ClampScalePercent(capture.ExportScalePercent)
            });
        }

        var annotatedPath = Path.Combine(paths.CapturesFolder, $"{capture.Number:00}-annotated.png");
        WriteAnnotatedOverview(exportedSource, annotations, annotatedPath);

        var exportedAnnotations = new List<Annotation>(annotations.Count);
        foreach (var annotation in annotations.OrderBy(item => item.Number))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rect = ClampRect(annotation.BoxRect, exportedSource.Width, exportedSource.Height);
            if (rect.Width < AnnotationCropPolicy.MinimumExportBoxSize ||
                rect.Height < AnnotationCropPolicy.MinimumExportBoxSize)
            {
                exportedAnnotations.Add(annotation);
                continue;
            }

            var annotationPath = Path.Combine(paths.CapturesFolder, $"{capture.Number:00}-annotation-{annotation.Number:00}.png");
            PortableImageOperations.WriteCrop(exportedSource, rect, annotationPath);
            exportedAnnotations.Add(annotation with { ImagePath = annotationPath });
        }

        return Task.FromResult(capture with
        {
            ScreenshotPath = primaryPath,
            CroppedPath = croppedPath,
            ThumbnailPath = thumbnailPath,
            AnnotatedImagePath = annotatedPath,
            Annotations = exportedAnnotations,
            PrivacyMasks = privacyMasks,
            ExportScalePercent = ClampScalePercent(capture.ExportScalePercent)
        });
    }

    private static SKBitmap CreateExportedSource(SKBitmap source, AnnotationCapture capture)
    {
        // Redaction intentionally happens at source resolution. Scaling first could
        // blend private source pixels into the edge of a mask.
        using var redacted = PortableImageOperations.Clone(source);
        using (var canvas = new SKCanvas(redacted))
        {
            foreach (var mask in capture.PrivacyMasks ?? [])
            {
                DrawPrivacyMask(canvas, mask.BoxRect, source.Width, source.Height);
            }
        }

        var scalePercent = ClampScalePercent(capture.ExportScalePercent);
        if (scalePercent == 100)
        {
            return PortableImageOperations.Clone(redacted);
        }

        var width = Math.Max(1, (int)Math.Round(redacted.Width * scalePercent / 100d));
        var height = Math.Max(1, (int)Math.Round(redacted.Height * scalePercent / 100d));
        return PortableImageOperations.Resize(redacted, width, height);
    }

    private static void DrawPrivacyMask(SKCanvas canvas, RectInt mask, int width, int height)
    {
        var rect = ClampRect(mask, width, height);
        var skRect = PortableImageOperations.ToSkRect(rect);
        using var fill = new SKPaint
        {
            Color = SKColors.Black,
            IsAntialias = false,
            Style = SKPaintStyle.Fill
        };
        canvas.DrawRect(skRect, fill);

        var fontSize = Math.Clamp(Math.Min(rect.Width / 8f, rect.Height / 3f), 8f, 18f);
        using var text = PortableImageOperations.CreateTextPaint(fontSize, SKColors.White);
        var label = "Privacy mask";
        var measuredWidth = text.MeasureText(label);
        if (measuredWidth > rect.Width - 4)
        {
            text.TextSize = Math.Max(6, text.TextSize * Math.Max(0.1f, (rect.Width - 4) / measuredWidth));
        }

        var metrics = text.FontMetrics;
        var x = rect.X + rect.Width / 2f;
        var y = rect.Y + rect.Height / 2f - (metrics.Ascent + metrics.Descent) / 2f;
        canvas.Save();
        canvas.ClipRect(skRect);
        canvas.DrawText(label, x, y, text);
        canvas.Restore();
    }

    private static IReadOnlyList<Annotation> ScaleAnnotations(IReadOnlyList<Annotation> annotations, int scalePercent)
    {
        var scale = ClampScalePercent(scalePercent) / 100d;
        return annotations
            .Select(annotation => annotation with { BoxRect = ScaleRect(annotation.BoxRect, scale) })
            .ToList();
    }

    private static IReadOnlyList<PrivacyMask>? ScalePrivacyMasks(IReadOnlyList<PrivacyMask>? masks, int scalePercent)
    {
        if (masks is null)
        {
            return null;
        }

        var scale = ClampScalePercent(scalePercent) / 100d;
        return masks
            .Select(mask => mask with { BoxRect = ScaleRect(mask.BoxRect, scale) })
            .ToList();
    }

    private static RectInt ScaleRect(RectInt rect, double scale)
    {
        return new RectInt(
            Math.Max(0, (int)Math.Round(rect.X * scale)),
            Math.Max(0, (int)Math.Round(rect.Y * scale)),
            Math.Max(1, (int)Math.Round(rect.Width * scale)),
            Math.Max(1, (int)Math.Round(rect.Height * scale)));
    }

    private static int ClampScalePercent(int scalePercent)
    {
        return Math.Clamp(scalePercent, 20, 100);
    }

    private static void WriteAnnotatedOverview(
        SKBitmap source,
        IReadOnlyList<Annotation> annotations,
        string path)
    {
        using var target = PortableImageOperations.Clone(source);
        using var canvas = new SKCanvas(target);
        foreach (var annotation in annotations.OrderBy(item => item.Number))
        {
            DrawAnnotation(canvas, annotation, source.Width, source.Height);
        }

        PortableImageOperations.SavePng(target, path);
    }

    private static void DrawAnnotation(SKCanvas canvas, Annotation annotation, int width, int height)
    {
        var rect = ClampRect(annotation.BoxRect, width, height);
        var skRect = PortableImageOperations.ToSkRect(rect);
        using var fill = new SKPaint
        {
            Color = new SKColor(224, 165, 54, 13),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        using var stroke = new SKPaint
        {
            Color = new SKColor(224, 165, 54, 242),
            IsAntialias = true,
            StrokeWidth = Math.Max(2, width / 900f),
            Style = SKPaintStyle.Stroke
        };
        canvas.DrawRect(skRect, fill);
        canvas.DrawRect(skRect, stroke);

        var badgeSize = Math.Max(26, width / 70);
        var badge = SKRect.Create(rect.X, rect.Y, badgeSize, badgeSize);
        using var badgeBrush = new SKPaint
        {
            Color = new SKColor(224, 165, 54, 245),
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };
        using var text = PortableImageOperations.CreateTextPaint(Math.Max(12, badgeSize * 0.48f), SKColors.Black);
        canvas.DrawRect(badge, badgeBrush);

        var label = annotation.Number.ToString();
        var metrics = text.FontMetrics;
        canvas.DrawText(
            label,
            badge.MidX,
            badge.MidY - (metrics.Ascent + metrics.Descent) / 2f,
            text);
    }

    private static RectInt ClampRect(RectInt rect, int width, int height)
    {
        var x = Math.Clamp(rect.X, 0, Math.Max(0, width - 1));
        var y = Math.Clamp(rect.Y, 0, Math.Max(0, height - 1));
        var rectWidth = Math.Clamp(rect.Width, 1, width - x);
        var rectHeight = Math.Clamp(rect.Height, 1, height - y);
        return new RectInt(x, y, rectWidth, rectHeight);
    }
}
