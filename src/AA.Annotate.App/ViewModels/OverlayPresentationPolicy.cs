namespace AA.Annotate.App.ViewModels;

internal sealed record OverlayPresentation(
    bool OverlayVisible,
    bool CaptureSurfaceVisible,
    bool ToolbarVisible,
    bool ToolbarEnabled);

internal static class OverlayPresentationPolicy
{
    public static OverlayPresentation Create(
        bool isCapturing,
        bool isDrawing,
        AnnotationInteractionMode mode,
        bool cropOverlayVisible,
        bool commentEditorVisible,
        bool idleWarningVisible,
        bool sessionConfirmationVisible,
        bool isTerminal)
    {
        if (isTerminal || IsTerminalMode(mode))
        {
            return Hidden;
        }

        if (isCapturing || mode == AnnotationInteractionMode.Capturing)
        {
            return Hidden;
        }

        var captureSurfaceVisible = IsCaptureSurfaceActive(
            isDrawing,
            mode,
            cropOverlayVisible,
            commentEditorVisible);
        var modalSurfaceVisible = idleWarningVisible || sessionConfirmationVisible;

        return new OverlayPresentation(
            OverlayVisible: captureSurfaceVisible || modalSurfaceVisible,
            CaptureSurfaceVisible: captureSurfaceVisible,
            ToolbarVisible: !modalSurfaceVisible,
            ToolbarEnabled: !modalSurfaceVisible);
    }

    internal static bool IsCaptureSurfaceActive(
        bool isDrawing,
        AnnotationInteractionMode mode,
        bool cropOverlayVisible,
        bool commentEditorVisible)
    {
        return isDrawing ||
            cropOverlayVisible ||
            commentEditorVisible ||
            mode is AnnotationInteractionMode.DrawingAnnotation or
                AnnotationInteractionMode.DrawingPrivacyMask or
                AnnotationInteractionMode.EditingCrop or
                AnnotationInteractionMode.AnnotationSelected;
    }

    private static bool IsTerminalMode(AnnotationInteractionMode mode)
    {
        return mode is AnnotationInteractionMode.Finishing or
            AnnotationInteractionMode.Cancelled or
            AnnotationInteractionMode.Completed;
    }

    private static OverlayPresentation Hidden { get; } = new(
        OverlayVisible: false,
        CaptureSurfaceVisible: false,
        ToolbarVisible: false,
        ToolbarEnabled: false);
}
