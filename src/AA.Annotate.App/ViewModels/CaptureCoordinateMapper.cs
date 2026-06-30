using AA.Annotate.Core.Geometry;

namespace AA.Annotate.App.ViewModels;

public static class CaptureCoordinateMapper
{
    public static RectInt ToPixelRect(RectInt viewRect, CaptureViewModel capture)
    {
        return ToPixelRect(viewRect, capture.ScreenshotPixelSize, capture.ViewportSize);
    }

    public static RectInt FromPixelRect(RectInt pixelRect, CaptureViewModel capture)
    {
        return FromPixelRect(pixelRect, capture.ScreenshotPixelSize, capture.ViewportSize);
    }

    public static RectInt ToPixelRect(RectInt viewRect, SizeInt screenshotSize, SizeInt viewportSize)
    {
        var viewport = NormalizeSize(viewportSize);
        viewRect = AnnotationRectPolicy.ClampToBounds(viewRect, viewport);
        var scaleX = screenshotSize.Width / (double)viewport.Width;
        var scaleY = screenshotSize.Height / (double)viewport.Height;
        var x = Math.Clamp((int)Math.Round(viewRect.X * scaleX), 0, screenshotSize.Width - 1);
        var y = Math.Clamp((int)Math.Round(viewRect.Y * scaleY), 0, screenshotSize.Height - 1);
        var width = Math.Clamp((int)Math.Round(viewRect.Width * scaleX), 1, screenshotSize.Width - x);
        var height = Math.Clamp((int)Math.Round(viewRect.Height * scaleY), 1, screenshotSize.Height - y);
        return new RectInt(x, y, width, height);
    }

    public static RectInt FromPixelRect(RectInt pixelRect, SizeInt screenshotSize, SizeInt viewportSize)
    {
        var screenshot = NormalizeSize(screenshotSize);
        var viewport = NormalizeSize(viewportSize);
        var scaleX = viewport.Width / (double)screenshot.Width;
        var scaleY = viewport.Height / (double)screenshot.Height;
        var x = Math.Clamp((int)Math.Round(pixelRect.X * scaleX), 0, viewport.Width - 1);
        var y = Math.Clamp((int)Math.Round(pixelRect.Y * scaleY), 0, viewport.Height - 1);
        var width = Math.Clamp((int)Math.Round(pixelRect.Width * scaleX), 1, viewport.Width - x);
        var height = Math.Clamp((int)Math.Round(pixelRect.Height * scaleY), 1, viewport.Height - y);
        return new RectInt(x, y, width, height);
    }

    private static SizeInt NormalizeSize(SizeInt size)
    {
        return new SizeInt(Math.Max(1, size.Width), Math.Max(1, size.Height));
    }
}
