using AA.Annotate.Core.Geometry;
using AA.Annotate.Core.Models;
using AA.Annotate.Core.Services;

namespace AA.Annotate.Core.Tests;

public sealed class ReviewMarkdownWriterTests
{
    [Fact]
    public void WriteCreatesAgentReadableCaptureSections()
    {
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
                    new RectInt(100, 120, 800, 600),
                    [new Annotation("a1", 1, new RectInt(40, 40, 320, 90), "Explain this area.", "captures/001/annotation-01.png")],
                    "captures/001/annotated.png")
            ]);

        var markdown = ReviewMarkdownWriter.Write(session);

        Assert.Contains("# Annotation Review", markdown);
        Assert.Contains("## Capture 1", markdown);
        Assert.Contains("Image: captures/001/cropped.png", markdown);
        Assert.Contains("Annotated image: captures/001/annotated.png", markdown);
        Assert.DoesNotContain("Full screenshot:", markdown);
        Assert.Contains("1. x=40, y=40, width=320, height=90", markdown);
        Assert.Contains("   Image: captures/001/annotation-01.png", markdown);
        Assert.DoesNotContain("Full screenshot box:", markdown);
        Assert.Contains("Explain this area.", markdown);
    }
}
