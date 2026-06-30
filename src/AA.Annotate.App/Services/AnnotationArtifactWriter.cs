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
using DrawingPen = System.Drawing.Pen;
using DrawingRectangle = System.Drawing.Rectangle;
using DrawingSolidBrush = System.Drawing.SolidBrush;
using DrawingStringFormat = System.Drawing.StringFormat;

namespace AA.Annotate.App.Services;

public sealed class AnnotationArtifactWriter : IAnnotationArtifactWriter
{
    public Task<AnnotationCapture> WriteAsync(
        SessionPaths paths,
        AnnotationCapture capture,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (capture.Annotations.Count == 0)
        {
            return Task.FromResult(capture);
        }

        Directory.CreateDirectory(paths.CapturesFolder);
        using var source = new DrawingBitmap(capture.ScreenshotPath);
        var annotatedPath = Path.Combine(paths.CapturesFolder, $"{capture.Number:00}-annotated.png");
        WriteAnnotatedOverview(source, capture, annotatedPath);

        var annotations = new List<Annotation>(capture.Annotations.Count);
        foreach (var annotation in capture.Annotations.OrderBy(item => item.Number))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rect = ClampRect(annotation.BoxRect, source.Width, source.Height);
            if (rect.Width < AnnotationCropPolicy.MinimumExportBoxSize ||
                rect.Height < AnnotationCropPolicy.MinimumExportBoxSize)
            {
                annotations.Add(annotation);
                continue;
            }

            var annotationPath = Path.Combine(paths.CapturesFolder, $"{capture.Number:00}-annotation-{annotation.Number:00}.png");
            WriteAnnotationImage(source, rect, annotationPath);
            annotations.Add(annotation with { ImagePath = annotationPath });
        }

        return Task.FromResult(capture with
        {
            AnnotatedImagePath = annotatedPath,
            Annotations = annotations
        });
    }

    private static void WriteAnnotatedOverview(DrawingBitmap source, AnnotationCapture capture, string path)
    {
        using var target = new DrawingBitmap(source.Width, source.Height);
        using var graphics = DrawingGraphics.FromImage(target);
        graphics.DrawImage(source, 0, 0, source.Width, source.Height);

        foreach (var annotation in capture.Annotations.OrderBy(item => item.Number))
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
