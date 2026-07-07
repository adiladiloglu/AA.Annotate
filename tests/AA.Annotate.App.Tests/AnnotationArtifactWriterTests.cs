using System.Drawing;
using System.Drawing.Imaging;
using AA.Annotate.App.Services;
using AA.Annotate.Core.Geometry;
using AA.Annotate.Core.Models;
using AA.Annotate.Core.Services;

namespace AA.Annotate.App.Tests;

public sealed class AnnotationArtifactWriterTests
{
    [Fact]
    public async Task WriteAsyncRedactsPrivacyMasksFromExportedImages()
    {
        var root = Path.Combine(Path.GetTempPath(), "AA.Annotate.Tests", Guid.NewGuid().ToString("N"));
        var paths = SessionPaths.FromFolder(root);
        Directory.CreateDirectory(paths.CapturesFolder);
        var screenshotPath = Path.Combine(paths.CapturesFolder, "01-screen.png");
        using (var bitmap = new Bitmap(100, 80))
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.LimeGreen);
            using var brush = new SolidBrush(Color.Red);
            graphics.FillRectangle(brush, 10, 10, 60, 30);
            bitmap.Save(screenshotPath, ImageFormat.Png);
        }

        var capture = new AnnotationCapture(
            "capture",
            1,
            new CaptureDisplay("display", "display", new RectInt(0, 0, 100, 80)),
            screenshotPath,
            null,
            screenshotPath,
            new SizeInt(100, 80),
            new RectInt(0, 0, 100, 80),
            new RectInt(0, 0, 100, 80),
            [new Annotation("a1", 1, new RectInt(0, 0, 80, 50), "contains private text")],
            PrivacyMasks: [new PrivacyMask("m1", new RectInt(10, 10, 60, 30))]);

        var result = await new AnnotationArtifactWriter().WriteAsync(paths, capture);

        using var primary = new Bitmap(result.ScreenshotPath);
        AssertPixelNear(Color.Black, primary.GetPixel(12, 12));
        Assert.True(ContainsLightPixel(primary, new Rectangle(10, 10, 60, 30)));
        AssertPixelNear(Color.LimeGreen, primary.GetPixel(5, 5));
        var annotation = Assert.Single(result.Annotations);
        using var snippet = new Bitmap(annotation.ImagePath!);
        AssertPixelNear(Color.Black, snippet.GetPixel(12, 12));
        Assert.True(ContainsLightPixel(snippet, new Rectangle(10, 10, 60, 30)));
    }

    [Fact]
    public async Task WriteAsyncDownsizesExportedImagesAndScalesCoordinates()
    {
        var root = Path.Combine(Path.GetTempPath(), "AA.Annotate.Tests", Guid.NewGuid().ToString("N"));
        var paths = SessionPaths.FromFolder(root);
        Directory.CreateDirectory(paths.CapturesFolder);
        var screenshotPath = Path.Combine(paths.CapturesFolder, "01-screen.png");
        using (var bitmap = new Bitmap(100, 80))
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.LimeGreen);
            bitmap.Save(screenshotPath, ImageFormat.Png);
        }

        var capture = new AnnotationCapture(
            "capture",
            1,
            new CaptureDisplay("display", "display", new RectInt(0, 0, 100, 80)),
            screenshotPath,
            null,
            screenshotPath,
            new SizeInt(100, 80),
            new RectInt(0, 0, 100, 80),
            new RectInt(0, 0, 100, 80),
            [new Annotation("a1", 1, new RectInt(20, 10, 40, 30), "button")],
            PrivacyMasks: [new PrivacyMask("m1", new RectInt(60, 20, 20, 20))],
            ExportScalePercent: 50);

        var result = await new AnnotationArtifactWriter().WriteAsync(paths, capture);

        using var primary = new Bitmap(result.ScreenshotPath);
        Assert.Equal(50, primary.Width);
        Assert.Equal(40, primary.Height);
        Assert.Equal(new RectInt(10, 5, 20, 15), result.Annotations.Single().BoxRect);
        Assert.Equal(new RectInt(30, 10, 10, 10), result.PrivacyMasks!.Single().BoxRect);
        using var snippet = new Bitmap(result.Annotations.Single().ImagePath!);
        Assert.Equal(20, snippet.Width);
        Assert.Equal(15, snippet.Height);
    }

    [Fact]
    public async Task WriteAsyncCreatesAnnotatedOverviewAndAnnotationSnippets()
    {
        var root = Path.Combine(Path.GetTempPath(), "AA.Annotate.Tests", Guid.NewGuid().ToString("N"));
        var paths = SessionPaths.FromFolder(root);
        Directory.CreateDirectory(paths.CapturesFolder);
        var screenshotPath = Path.Combine(paths.CapturesFolder, "01-screen.png");
        using (var bitmap = new Bitmap(160, 120))
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.FromArgb(32, 32, 32));
            bitmap.Save(screenshotPath, ImageFormat.Png);
        }

        var capture = new AnnotationCapture(
            "capture",
            1,
            new CaptureDisplay("display", "display", new RectInt(0, 0, 160, 120)),
            screenshotPath,
            null,
            screenshotPath,
            new SizeInt(160, 120),
            new RectInt(0, 0, 160, 120),
            new RectInt(0, 0, 160, 120),
            [new Annotation("a1", 1, new RectInt(20, 20, 60, 40), "button")]);

        var result = await new AnnotationArtifactWriter().WriteAsync(paths, capture);

        Assert.NotNull(result.AnnotatedImagePath);
        Assert.True(File.Exists(result.AnnotatedImagePath));
        var annotation = Assert.Single(result.Annotations);
        Assert.NotNull(annotation.ImagePath);
        Assert.True(File.Exists(annotation.ImagePath));
        using var snippet = new Bitmap(annotation.ImagePath);
        Assert.Equal(60, snippet.Width);
        Assert.Equal(40, snippet.Height);
    }

    [Fact]
    public async Task WriteAsyncAlwaysExportsPrimaryImageOutsideWorkingCaptures()
    {
        var root = Path.Combine(Path.GetTempPath(), "AA.Annotate.Tests", Guid.NewGuid().ToString("N"));
        var workingRoot = Path.Combine(root, "work");
        var exportRoot = Path.Combine(root, "export");
        var paths = SessionPaths.FromFolder(workingRoot, exportRoot);
        Directory.CreateDirectory(paths.WorkingCapturesFolder);
        var screenshotPath = Path.Combine(paths.WorkingCapturesFolder, "01-screen.png");
        using (var bitmap = new Bitmap(100, 80))
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.LimeGreen);
            bitmap.Save(screenshotPath, ImageFormat.Png);
        }

        var capture = new AnnotationCapture(
            "capture",
            1,
            new CaptureDisplay("display", "display", new RectInt(0, 0, 100, 80)),
            screenshotPath,
            null,
            screenshotPath,
            new SizeInt(100, 80),
            new RectInt(0, 0, 100, 80),
            new RectInt(0, 0, 100, 80),
            [new Annotation("a1", 1, new RectInt(20, 20, 40, 30), "button")]);

        var result = await new AnnotationArtifactWriter().WriteAsync(paths, capture);

        Assert.Equal(Path.Combine(paths.ExportCapturesFolder, "01-export.png"), result.ScreenshotPath);
        Assert.Equal(result.ScreenshotPath, result.ThumbnailPath);
        Assert.DoesNotContain(paths.WorkingCapturesFolder, result.ScreenshotPath, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(result.ScreenshotPath));
    }

    private static void AssertPixelNear(Color expected, Color actual)
    {
        Assert.InRange(actual.R, Math.Max(0, expected.R - 3), Math.Min(255, expected.R + 3));
        Assert.InRange(actual.G, Math.Max(0, expected.G - 3), Math.Min(255, expected.G + 3));
        Assert.InRange(actual.B, Math.Max(0, expected.B - 3), Math.Min(255, expected.B + 3));
    }

    private static bool ContainsLightPixel(Bitmap bitmap, Rectangle area)
    {
        for (var y = area.Top; y < area.Bottom; y++)
        {
            for (var x = area.Left; x < area.Right; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.R > 180 && pixel.G > 180 && pixel.B > 180)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
