namespace AA.Annotate.App.ViewModels;

internal static class CaptureSourceCleanupPolicy
{
    public static bool ShouldDeleteRawSource(CaptureViewModel capture, int exportScalePercent)
    {
        return CaptureCropProjector.IsCropped(capture) ||
            capture.PrivacyMasks.Count > 0 ||
            ExportScalePercentParser.Clamp(exportScalePercent) < 100;
    }
}
