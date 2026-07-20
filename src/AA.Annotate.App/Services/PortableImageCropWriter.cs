using AA.Annotate.Core.Geometry;

namespace AA.Annotate.App.Services;

public sealed class PortableImageCropWriter
{
    public string WriteCrop(string sourcePath, string destinationPath, RectInt cropRect)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        using var source = PortableImageOperations.Load(sourcePath);
        var crop = Clamp(cropRect, source.Width, source.Height);
        PortableImageOperations.WriteCrop(source, crop, destinationPath);
        return destinationPath;
    }

    private static RectInt Clamp(RectInt crop, int width, int height)
    {
        var x = Math.Clamp(crop.X, 0, width - 1);
        var y = Math.Clamp(crop.Y, 0, height - 1);
        var cropWidth = Math.Clamp(crop.Width, 1, width - x);
        var cropHeight = Math.Clamp(crop.Height, 1, height - y);
        return new RectInt(x, y, cropWidth, cropHeight);
    }
}
