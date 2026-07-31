using Avalonia.Input;

namespace AA.Annotate.App.ViewModels;

internal static class OverlayCreationGesturePolicy
{
    public static string? GetExistingBoxHint(AnnotationInteractionMode mode)
    {
        return mode switch
        {
            AnnotationInteractionMode.DrawingAnnotation =>
                "Ctrl+drag to draw a new annotation through this box",
            AnnotationInteractionMode.DrawingPrivacyMask =>
                "Ctrl+drag to draw a new privacy mask through this box",
            _ => null
        };
    }

    public static bool ShouldForceNewBox(
        AnnotationInteractionMode mode,
        KeyModifiers modifiers,
        bool isLeftButtonPressed)
    {
        return isLeftButtonPressed
            && modifiers.HasFlag(KeyModifiers.Control)
            && mode is AnnotationInteractionMode.DrawingAnnotation
                or AnnotationInteractionMode.DrawingPrivacyMask;
    }
}
