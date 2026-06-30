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
}
