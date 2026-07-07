namespace AA.Annotate.App.ViewModels;

public enum AnnotationInteractionMode
{
    Idle,
    Capturing,
    Editing,
    DrawingAnnotation,
    DrawingPrivacyMask,
    EditingCrop,
    AnnotationSelected,
    CaptureDropdownOpen,
    Finishing,
    Cancelled,
    Completed
}
