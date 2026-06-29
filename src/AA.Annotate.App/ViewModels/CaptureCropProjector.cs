using AA.Annotate.Core.Geometry;

namespace AA.Annotate.App.ViewModels;

public static class CaptureCropProjector
{
    public static RectInt ToViewportCrop(CaptureViewModel capture)
    {
        return CaptureCoordinateMapper.FromPixelRect(capture.CropPixelRect, capture);
    }

    public static void CommitViewportCrop(CaptureViewModel capture, RectInt viewportCrop)
    {
        capture.CropRect = viewportCrop;
        capture.CropPixelRect = CaptureCoordinateMapper.ToPixelRect(viewportCrop, capture);
    }

    public static bool IsCropped(CaptureViewModel capture)
    {
        return CropViewport.IsCropped(capture.CropPixelRect, capture.ScreenshotPixelSize);
    }
}
