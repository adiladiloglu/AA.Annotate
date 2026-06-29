namespace AA.Annotate.App.ViewModels;

internal enum CaptureDependentToolAction
{
    UseCurrentCapture,
    CaptureFirst
}

internal static class CaptureDependentToolPolicy
{
    public static CaptureDependentToolAction SelectAction(bool hasCapture)
    {
        return hasCapture
            ? CaptureDependentToolAction.UseCurrentCapture
            : CaptureDependentToolAction.CaptureFirst;
    }
}
