using AA.Annotate.Core.Geometry;

namespace AA.Annotate.Core.Tests;

public sealed class CropViewportTests
{
    [Fact]
    public void NormalizeConvertsFullPixelCropToFullViewportCrop()
    {
        var crop = CropViewport.Normalize(
            new RectInt(0, 0, 2560, 1600),
            new SizeInt(2560, 1600),
            new SizeInt(1707, 1067));

        Assert.Equal(new RectInt(0, 0, 1707, 1067), crop);
    }

    [Fact]
    public void IsCroppedReturnsFalseForFullViewportCrop()
    {
        var cropped = CropViewport.IsCropped(
            new RectInt(0, 0, 1707, 1067),
            new SizeInt(1707, 1067));

        Assert.False(cropped);
    }

    [Fact]
    public void IsCroppedReturnsTrueForSmallerViewportCrop()
    {
        var cropped = CropViewport.IsCropped(
            new RectInt(100, 80, 900, 600),
            new SizeInt(1707, 1067));

        Assert.True(cropped);
    }

    [Fact]
    public void NormalizeClampsCropToViewportBounds()
    {
        var crop = CropViewport.Normalize(
            new RectInt(100, 80, 2000, 1200),
            new SizeInt(2560, 1600),
            new SizeInt(1707, 1067));

        Assert.Equal(new RectInt(100, 80, 1607, 987), crop);
    }
}
