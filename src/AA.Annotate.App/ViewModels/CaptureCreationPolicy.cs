namespace AA.Annotate.App.ViewModels;

internal static class CaptureCreationPolicy
{
    public static bool CanUseCaptureControls(AnnotationInteractionMode mode, bool cropOverlayVisible)
    {
        if (cropOverlayVisible)
        {
            return false;
        }

        return mode is AnnotationInteractionMode.Idle
            or AnnotationInteractionMode.Editing
            or AnnotationInteractionMode.CaptureDropdownOpen;
    }
}
