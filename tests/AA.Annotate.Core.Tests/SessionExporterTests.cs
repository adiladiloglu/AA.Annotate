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
}
