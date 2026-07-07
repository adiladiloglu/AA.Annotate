using System.Drawing.Imaging;
using AA.Annotate.Core.Geometry;
using AA.Annotate.Core.Models;
using AA.Annotate.Core.Services;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingBrushes = System.Drawing.Brushes;
using DrawingColor = System.Drawing.Color;
using DrawingFont = System.Drawing.Font;
using DrawingFontStyle = System.Drawing.FontStyle;
using DrawingGraphics = System.Drawing.Graphics;
using DrawingGraphicsUnit = System.Drawing.GraphicsUnit;
using DrawingInterpolationMode = System.Drawing.Drawing2D.InterpolationMode;
using DrawingPen = System.Drawing.Pen;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSolidBrush = System.Drawing.SolidBrush;
using DrawingStringFormat = System.Drawing.StringFormat;
using DrawingStringTrimming = System.Drawing.StringTrimming;

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
        using var source = new DrawingBitmap(capture.ScreenshotPath);
        using var exportedSource = CreateExportedSource(source, capture);
        var annotations = ScaleAnnotations(capture.Annotations, capture.ExportScalePercent);
        var privacyMasks = ScalePrivacyMasks(capture.PrivacyMasks, capture.ExportScalePercent);
        var primaryPath = Path.Combine(paths.CapturesFolder, $"{capture.Number:00}-export.png");
        exportedSource.Save(primaryPath, ImageFormat.Png);
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
            WriteAnnotationImage(exportedSource, rect, annotationPath);
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

    private static DrawingBitmap CreateExportedSource(DrawingBitmap source, AnnotationCapture capture)
    {
        var scalePercent = ClampScalePercent(capture.ExportScalePercent);
        using var redacted = new DrawingBitmap(source.Width, source.Height);
        using (var graphics = DrawingGraphics.FromImage(redacted))
        {
            graphics.DrawImage(source, 0, 0, source.Width, source.Height);
            foreach (var mask in capture.PrivacyMasks ?? [])
            {
                DrawPrivacyMask(graphics, mask.BoxRect, source.Width, source.Height);
            }
        }

        if (scalePercent == 100)
        {
            return new DrawingBitmap(redacted);
        }

        var width = Math.Max(1, (int)Math.Round(redacted.Width * scalePercent / 100d));
        var height = Math.Max(1, (int)Math.Round(redacted.Height * scalePercent / 100d));
        var resized = new DrawingBitmap(width, height);
        using var resizeGraphics = DrawingGraphics.FromImage(resized);
        resizeGraphics.InterpolationMode = DrawingInterpolationMode.HighQualityBicubic;
        resizeGraphics.DrawImage(redacted, new DrawingRectangle(0, 0, width, height));
        return resized;
    }

    private static void DrawPrivacyMask(DrawingGraphics graphics, RectInt mask, int width, int height)
    {
        var rect = ClampRect(mask, width, height);
        using var fill = new DrawingSolidBrush(DrawingColor.Black);
        graphics.FillRectangle(fill, rect);

        var fontSize = Math.Clamp(Math.Min(rect.Width / 8f, rect.Height / 3f), 8f, 18f);
        using var font = new DrawingFont("Segoe UI", fontSize, DrawingFontStyle.Bold);
        using var format = new DrawingStringFormat
        {
            Alignment = System.Drawing.StringAlignment.Center,
            LineAlignment = System.Drawing.StringAlignment.Center,
            Trimming = DrawingStringTrimming.EllipsisCharacter,
            FormatFlags = System.Drawing.StringFormatFlags.NoWrap
        };

        graphics.DrawString("Privacy mask", font, DrawingBrushes.White, rect, format);
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

    private static void WriteAnnotatedOverview(DrawingBitmap source, IReadOnlyList<Annotation> annotations, string path)
    {
        using var target = new DrawingBitmap(source.Width, source.Height);
        using var graphics = DrawingGraphics.FromImage(target);
        graphics.DrawImage(source, 0, 0, source.Width, source.Height);

        foreach (var annotation in annotations.OrderBy(item => item.Number))
        {
            DrawAnnotation(graphics, annotation, source.Width, source.Height);
        }

        target.Save(path, ImageFormat.Png);
    }

    private static void WriteAnnotationImage(DrawingBitmap source, DrawingRectangle rect, string path)
    {
        using var target = new DrawingBitmap(rect.Width, rect.Height);
        using var graphics = DrawingGraphics.FromImage(target);
        graphics.DrawImage(
            source,
            new DrawingRectangle(0, 0, rect.Width, rect.Height),
            rect,
            DrawingGraphicsUnit.Pixel);
        target.Save(path, ImageFormat.Png);
    }

    private static void DrawAnnotation(DrawingGraphics graphics, Annotation annotation, int width, int height)
    {
        var rect = ClampRect(annotation.BoxRect, width, height);
        using var pen = new DrawingPen(DrawingColor.FromArgb(242, 224, 165, 54), Math.Max(2, width / 900));
        using var fill = new DrawingSolidBrush(DrawingColor.FromArgb(13, 224, 165, 54));
        graphics.FillRectangle(fill, rect);
        graphics.DrawRectangle(pen, rect);

        var badgeSize = Math.Max(26, width / 70);
        var badge = new DrawingRectangle(rect.X, rect.Y, badgeSize, badgeSize);
        using var badgeBrush = new DrawingSolidBrush(DrawingColor.FromArgb(245, 224, 165, 54));
        using var font = new DrawingFont("Segoe UI", Math.Max(12, badgeSize * 0.48f), DrawingFontStyle.Bold);
        using var format = new DrawingStringFormat
        {
            Alignment = System.Drawing.StringAlignment.Center,
            LineAlignment = System.Drawing.StringAlignment.Center
        };

        graphics.FillRectangle(badgeBrush, badge);
        graphics.DrawString(annotation.Number.ToString(), font, DrawingBrushes.Black, badge, format);
    }

    private static DrawingRectangle ClampRect(RectInt rect, int width, int height)
    {
        var x = Math.Clamp(rect.X, 0, Math.Max(0, width - 1));
        var y = Math.Clamp(rect.Y, 0, Math.Max(0, height - 1));
        var rectWidth = Math.Clamp(rect.Width, 1, width - x);
        var rectHeight = Math.Clamp(rect.Height, 1, height - y);
        return new DrawingRectangle(x, y, rectWidth, rectHeight);
    }
}
