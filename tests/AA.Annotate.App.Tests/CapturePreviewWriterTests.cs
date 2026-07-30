using AA.Annotate.App.Services;
using SkiaSharp;

namespace AA.Annotate.App.Tests;

public sealed class CapturePreviewWriterTests
{
    [Fact]
    public void WriteAlwaysResamplesFromOriginalScreenshot()
    {
        var root = Path.Combine(Path.GetTempPath(), "AA.Annotate.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var originalPath = Path.Combine(root, "screen.png");
        var previewPath = Path.Combine(root, "preview.png");
        WriteImage(originalPath, 101, 81);
        var writer = new CapturePreviewWriter();

        var first = writer.Write(originalPath, previewPath, 50);
        using (var firstBitmap = SKBitmap.Decode(first.ImagePath))
        {
            Assert.NotNull(firstBitmap);
            Assert.Equal(50, firstBitmap.Width);
            Assert.Equal(40, firstBitmap.Height);
        }

        var second = writer.Write(originalPath, previewPath, 75);
        using var secondBitmap = SKBitmap.Decode(second.ImagePath);
        using var originalBitmap = SKBitmap.Decode(originalPath);
        Assert.NotNull(secondBitmap);
        Assert.NotNull(originalBitmap);
        Assert.Equal(76, secondBitmap.Width);
        Assert.Equal(61, secondBitmap.Height);
        Assert.Equal(101, originalBitmap.Width);
        Assert.Equal(81, originalBitmap.Height);
    }

    private static void WriteImage(string path, int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }
}
