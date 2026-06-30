using System.Text.Json;
using AA.Annotate.Core.Geometry;
using AA.Annotate.Core.Models;
using AA.Annotate.Core.Serialization;
using AA.Annotate.Core.Services;

namespace AA.Annotate.Core.Tests;

public sealed class SessionExporterTests
{
    [Fact]
    public async Task ExportWritesReviewAndAnnotationsJson()
    {
        var root = Path.Combine(Path.GetTempPath(), "AA.Annotate.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var paths = SessionPaths.FromFolder(root);
        var session = new AnnotationSession(
            "s1",
            DateTimeOffset.Parse("2026-06-28T15:55:00Z"),
            DateTimeOffset.Parse("2026-06-28T15:59:00Z"),
            SessionStatus.Completed,
            [
                new AnnotationCapture(
                    "001",
                    1,
                    new CaptureDisplay("DISPLAY1", "Primary", new RectInt(0, 0, 1920, 1080)),
                    "captures/001/screenshot.png",
                    null,
                    "captures/001/thumbnail.png",
                    new SizeInt(1920, 1080),
                    new RectInt(0, 0, 1920, 1080),
                    null,
                    [])
            ]);

        var exporter = new SessionExporter();
        await exporter.ExportAsync(paths, session);

        Assert.True(File.Exists(paths.ReviewMarkdownPath));
        Assert.True(File.Exists(paths.AnnotationsJsonPath));
        var json = await File.ReadAllTextAsync(paths.AnnotationsJsonPath);
        var exported = JsonSerializer.Deserialize<AnnotationSession>(json, SessionJsonOptions.Create());
        Assert.Equal("s1", exported!.SessionId);
    }

    [Fact]
    public async Task ExportRemovesAnnotationsOutsideCropAndClipsOverlappingAnnotations()
    {
        var root = Path.Combine(Path.GetTempPath(), "AA.Annotate.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var paths = SessionPaths.FromFolder(root);
        var session = new AnnotationSession(
            "s1",
            DateTimeOffset.Parse("2026-06-28T15:55:00Z"),
            DateTimeOffset.Parse("2026-06-28T15:59:00Z"),
            SessionStatus.Completed,
            [
                new AnnotationCapture(
                    "001",
                    1,
                    new CaptureDisplay("DISPLAY1", "Primary", new RectInt(0, 0, 1920, 1080)),
                    "captures/001/screenshot.png",
                    "captures/001/cropped.png",
                    "captures/001/thumbnail.png",
                    new SizeInt(1920, 1080),
                    new RectInt(0, 0, 1920, 1080),
                    new RectInt(100, 100, 300, 200),
                    [
                        new Annotation("inside", 1, new RectInt(150, 120, 50, 40), "inside crop"),
                        new Annotation("outside", 2, new RectInt(10, 10, 20, 20), "outside crop"),
                        new Annotation("partial", 3, new RectInt(80, 150, 50, 80), "crosses crop edge")
                    ])
            ]);

        var exporter = new SessionExporter();
        await exporter.ExportAsync(paths, session);

        var json = await File.ReadAllTextAsync(paths.AnnotationsJsonPath);
        var exported = JsonSerializer.Deserialize<AnnotationSession>(json, SessionJsonOptions.Create());
        var capture = exported!.Captures.Single();
        Assert.Equal(new SizeInt(1920, 1080), capture.ScreenshotPixelSize);
        Assert.Equal(new RectInt(100, 100, 300, 200), capture.CropRect);

        var annotations = capture.Annotations;
        Assert.Equal(2, annotations.Count);
        Assert.DoesNotContain(annotations, annotation => annotation.AnnotationId == "outside");
        Assert.Equal(new RectInt(50, 20, 50, 40), annotations.Single(annotation => annotation.AnnotationId == "inside").BoxRect);
        Assert.Equal(new RectInt(0, 50, 30, 80), annotations.Single(annotation => annotation.AnnotationId == "partial").BoxRect);

        var markdown = await File.ReadAllTextAsync(paths.ReviewMarkdownPath);
        Assert.Contains("1. x=50, y=20, width=50, height=40", markdown);
        Assert.Contains("3. x=0, y=50, width=30, height=80", markdown);
        Assert.DoesNotContain("outside crop", markdown);
        Assert.DoesNotContain("Full screenshot box:", markdown);
    }
}
