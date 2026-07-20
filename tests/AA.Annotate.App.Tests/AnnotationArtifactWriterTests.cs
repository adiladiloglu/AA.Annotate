using AA.Annotate.App.Services;
using AA.Annotate.Core.Geometry;
using AA.Annotate.Core.Models;
using AA.Annotate.Core.Services;
using SkiaSharp;

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
        WriteSolidImage(screenshotPath, 100, 80, SKColors.LimeGreen, new RectInt(10, 10, 60, 30), SKColors.Red);

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

        using var primary = Load(result.ScreenshotPath);
        AssertPixelNear(SKColors.Black, primary.GetPixel(12, 12));
        Assert.True(ContainsLightPixel(primary, new RectInt(10, 10, 60, 30)));
        AssertPixelNear(SKColors.LimeGreen, primary.GetPixel(5, 5));
        var annotation = Assert.Single(result.Annotations);
        using var snippet = Load(annotation.ImagePath!);
        AssertPixelNear(SKColors.Black, snippet.GetPixel(12, 12));
        Assert.True(ContainsLightPixel(snippet, new RectInt(10, 10, 60, 30)));
    }

    [Fact]
    public async Task WriteAsyncDownsizesExportedImagesAndScalesCoordinates()
    {
        var root = Path.Combine(Path.GetTempPath(), "AA.Annotate.Tests", Guid.NewGuid().ToString("N"));
        var paths = SessionPaths.FromFolder(root);
        Directory.CreateDirectory(paths.CapturesFolder);
        var screenshotPath = Path.Combine(paths.CapturesFolder, "01-screen.png");
        WriteSolidImage(screenshotPath, 100, 80, SKColors.LimeGreen);

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

        using var primary = Load(result.ScreenshotPath);
        Assert.Equal(50, primary.Width);
        Assert.Equal(40, primary.Height);
        Assert.Equal(new RectInt(10, 5, 20, 15), result.Annotations.Single().BoxRect);
        Assert.Equal(new RectInt(30, 10, 10, 10), result.PrivacyMasks!.Single().BoxRect);
        using var snippet = Load(result.Annotations.Single().ImagePath!);
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
        WriteSolidImage(screenshotPath, 160, 120, new SKColor(32, 32, 32));

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
        using var snippet = Load(annotation.ImagePath);
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
        WriteSolidImage(screenshotPath, 100, 80, SKColors.LimeGreen);

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
        Assert.DoesNotContain(
            paths.WorkingCapturesFolder,
            result.ScreenshotPath,
            OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        Assert.True(File.Exists(result.ScreenshotPath));
    }

    private static SKBitmap Load(string path)
    {
        return SKBitmap.Decode(path) ?? throw new InvalidDataException($"Could not decode {path}.");
    }

    private static void WriteSolidImage(
        string path,
        int width,
        int height,
        SKColor color,
        RectInt? accentRect = null,
        SKColor? accentColor = null)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(color);
        if (accentRect is { } rect && accentColor is { } fillColor)
        {
            using var paint = new SKPaint { Color = fillColor };
            canvas.DrawRect(SKRect.Create(rect.X, rect.Y, rect.Width, rect.Height), paint);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }

    private static void AssertPixelNear(SKColor expected, SKColor actual)
    {
        Assert.InRange(actual.Red, Math.Max(0, expected.Red - 3), Math.Min(255, expected.Red + 3));
        Assert.InRange(actual.Green, Math.Max(0, expected.Green - 3), Math.Min(255, expected.Green + 3));
        Assert.InRange(actual.Blue, Math.Max(0, expected.Blue - 3), Math.Min(255, expected.Blue + 3));
    }

    private static bool ContainsLightPixel(SKBitmap bitmap, RectInt area)
    {
        for (var y = area.Y; y < area.Bottom; y++)
        {
            for (var x = area.X; x < area.Right; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.Red > 180 && pixel.Green > 180 && pixel.Blue > 180)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
