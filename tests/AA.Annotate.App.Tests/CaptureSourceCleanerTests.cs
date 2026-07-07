using AA.Annotate.App.ViewModels;
using AA.Annotate.Core.Geometry;
using AA.Annotate.Platform;

namespace AA.Annotate.App.Tests;

public sealed class CaptureSourceCleanerTests
{
    [Fact]
    public void DeleteRawSourcesDeletesScreenshotAndThumbnailForPrivacyMaskedCapture()
    {
        var root = Path.Combine(Path.GetTempPath(), "AA.Annotate.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var screenshotPath = Path.Combine(root, "screen.png");
        var thumbnailPath = Path.Combine(root, "thumb.png");
        File.WriteAllText(screenshotPath, "raw");
        File.WriteAllText(thumbnailPath, "raw");
        var capture = CreateCapture(screenshotPath, thumbnailPath);
        capture.PrivacyMasks.Add(new PrivacyMaskViewModel("mask", new RectInt(10, 10, 20, 20)));

        CaptureSourceCleaner.DeleteRawSources([capture], exportScalePercent: 100);

        Assert.False(File.Exists(screenshotPath));
        Assert.False(File.Exists(thumbnailPath));
    }

    [Fact]
    public void DeleteRawSourcesKeepsScreenshotAndThumbnailForUnmodifiedFullSizeCapture()
    {
        var root = Path.Combine(Path.GetTempPath(), "AA.Annotate.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var screenshotPath = Path.Combine(root, "screen.png");
        var thumbnailPath = Path.Combine(root, "thumb.png");
        File.WriteAllText(screenshotPath, "raw");
        File.WriteAllText(thumbnailPath, "raw");
        var capture = CreateCapture(screenshotPath, thumbnailPath);

        CaptureSourceCleaner.DeleteRawSources([capture], exportScalePercent: 100);

        Assert.True(File.Exists(screenshotPath));
        Assert.True(File.Exists(thumbnailPath));
    }

    private static CaptureViewModel CreateCapture(string screenshotPath, string thumbnailPath)
    {
        return new CaptureViewModel(
            "capture",
            1,
            new DisplayDescriptor("display", "display", new RectInt(0, 0, 100, 100), IsPrimary: true),
            screenshotPath,
            thumbnailPath,
            new SizeInt(100, 100),
            new RectInt(0, 0, 100, 100),
            isSelected: true);
    }
}
