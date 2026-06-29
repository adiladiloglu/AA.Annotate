namespace AA.Annotate.App.ViewModels;

internal static class CaptureCropInheritancePolicy
{
    public static bool TryCopyCrop(CaptureViewModel? source, CaptureViewModel target)
    {
        if (source is null ||
            !CaptureCropProjector.IsCropped(source) ||
            !IsSameScreen(source, target))
        {
            return false;
        }

        target.CropPixelRect = source.CropPixelRect;
        target.ViewportSize = source.ViewportSize;
        target.CropRect = CaptureCropProjector.ToViewportCrop(target);
        return true;
    }

    private static bool IsSameScreen(CaptureViewModel source, CaptureViewModel target)
    {
        return string.Equals(source.Display.Id, target.Display.Id, StringComparison.OrdinalIgnoreCase) &&
            source.ScreenBounds == target.ScreenBounds &&
            source.ScreenshotPixelSize == target.ScreenshotPixelSize;
    }
}
