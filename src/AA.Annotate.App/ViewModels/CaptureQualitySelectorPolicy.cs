namespace AA.Annotate.App.ViewModels;

internal static class CaptureQualitySelectorPolicy
{
    internal static bool ShouldShow(
        bool hasCurrentCapture,
        OverlayPresentation presentation)
    {
        return hasCurrentCapture && presentation.CaptureSurfaceVisible;
    }
}
