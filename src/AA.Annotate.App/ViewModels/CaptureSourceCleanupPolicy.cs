namespace AA.Annotate.App.ViewModels;

internal static class CaptureSourceCleanupPolicy
{
    public static bool ShouldDeleteRawSource(CaptureViewModel capture)
    {
        return CaptureCropProjector.IsCropped(capture) ||
            capture.PrivacyMasks.Count > 0 ||
            capture.ExportScalePercent < 100;
    }
}
