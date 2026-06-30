namespace AA.Annotate.App.ViewModels;

public static class InteractionSurfacePolicy
{
    public static bool ShouldUseFullscreen(
        bool isCapturing,
        bool isDrawing,
        AnnotationInteractionMode mode,
        bool cropOverlayVisible,
        bool commentEditorVisible,
        bool cropIsActive)
    {
        return isCapturing ||
            isDrawing ||
            mode is AnnotationInteractionMode.DrawingAnnotation or
                AnnotationInteractionMode.EditingCrop or
                AnnotationInteractionMode.AnnotationSelected ||
            cropOverlayVisible ||
            commentEditorVisible ||
            cropIsActive;
    }

    public static bool ShouldRenderCaptureSurface(
        bool isDrawing,
        AnnotationInteractionMode mode,
        bool cropOverlayVisible,
        bool commentEditorVisible)
    {
        return isDrawing ||
            cropOverlayVisible ||
            commentEditorVisible ||
            mode is AnnotationInteractionMode.DrawingAnnotation or
                AnnotationInteractionMode.EditingCrop or
                AnnotationInteractionMode.AnnotationSelected;
    }
}
