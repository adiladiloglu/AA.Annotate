namespace AA.Annotate.Platform;

public sealed class ScreenCaptureResult
{
    private ScreenCaptureResult(
        ScreenCaptureOutcome outcome,
        CapturedScreen? capturedScreen,
        string? errorMessage)
    {
        Outcome = outcome;
        CapturedScreen = capturedScreen;
        ErrorMessage = errorMessage;
    }

    public ScreenCaptureOutcome Outcome { get; }

    public CapturedScreen? CapturedScreen { get; }

    public string? ErrorMessage { get; }

    public bool IsCompleted => Outcome == ScreenCaptureOutcome.Completed;

    public static ScreenCaptureResult Completed(CapturedScreen capturedScreen)
    {
        ArgumentNullException.ThrowIfNull(capturedScreen);
        return new ScreenCaptureResult(ScreenCaptureOutcome.Completed, capturedScreen, errorMessage: null);
    }

    public static ScreenCaptureResult Cancelled(string? message = null)
    {
        return Incomplete(ScreenCaptureOutcome.Cancelled, message);
    }

    public static ScreenCaptureResult PermissionDenied(string? message = null)
    {
        return Incomplete(ScreenCaptureOutcome.PermissionDenied, message);
    }

    public static ScreenCaptureResult DisplayDisconnected(string? message = null)
    {
        return Incomplete(ScreenCaptureOutcome.DisplayDisconnected, message);
    }

    public static ScreenCaptureResult RestartRequired(string? message = null)
    {
        return Incomplete(ScreenCaptureOutcome.RestartRequired, message);
    }

    public static ScreenCaptureResult Unavailable(string? message = null)
    {
        return Incomplete(ScreenCaptureOutcome.Unavailable, message);
    }

    public static ScreenCaptureResult Failed(string? message = null)
    {
        return Incomplete(ScreenCaptureOutcome.Failed, message);
    }

    private static ScreenCaptureResult Incomplete(ScreenCaptureOutcome outcome, string? message)
    {
        return new ScreenCaptureResult(outcome, capturedScreen: null, message);
    }
}
