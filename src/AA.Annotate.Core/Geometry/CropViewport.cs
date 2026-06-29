namespace AA.Annotate.Core.Geometry;

public static class CropViewport
{
    public static RectInt Normalize(RectInt crop, SizeInt screenshotSize, SizeInt viewportSize)
    {
        var viewport = NormalizeSize(viewportSize);
        if (IsFullFrame(crop, NormalizeSize(screenshotSize)))
        {
            return new RectInt(0, 0, viewport.Width, viewport.Height);
        }

        return ClampToViewport(crop, viewport);
    }

    public static bool IsCropped(RectInt crop, SizeInt viewportSize)
    {
        var viewport = NormalizeSize(viewportSize);
        var normalized = ClampToViewport(crop, viewport);
        return normalized.X != 0 ||
            normalized.Y != 0 ||
            normalized.Width != viewport.Width ||
            normalized.Height != viewport.Height;
    }

    private static RectInt ClampToViewport(RectInt crop, SizeInt viewport)
    {
        var x = Math.Clamp(crop.X, 0, viewport.Width - 1);
        var y = Math.Clamp(crop.Y, 0, viewport.Height - 1);
        var width = Math.Clamp(crop.Width, 1, viewport.Width - x);
        var height = Math.Clamp(crop.Height, 1, viewport.Height - y);
        return new RectInt(x, y, width, height);
    }

    private static bool IsFullFrame(RectInt crop, SizeInt size)
    {
        return crop.X == 0 &&
            crop.Y == 0 &&
            crop.Width == size.Width &&
            crop.Height == size.Height;
    }

    private static SizeInt NormalizeSize(SizeInt size)
    {
        return new SizeInt(Math.Max(1, size.Width), Math.Max(1, size.Height));
    }
}
