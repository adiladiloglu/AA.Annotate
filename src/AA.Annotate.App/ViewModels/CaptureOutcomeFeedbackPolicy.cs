using AA.Annotate.Platform;

namespace AA.Annotate.App.ViewModels;

internal sealed record CaptureOutcomeFeedback(string Title, string Message);

internal static class CaptureOutcomeFeedbackPolicy
{
    public static CaptureOutcomeFeedback? Create(
        ScreenCaptureOutcome outcome,
        string? safeDetail = null)
    {
        var feedback = outcome switch
        {
            ScreenCaptureOutcome.Completed => null,
            ScreenCaptureOutcome.Cancelled => null,
            ScreenCaptureOutcome.PermissionDenied => new CaptureOutcomeFeedback(
                "Screen recording permission needed",
                "Allow screen recording for AA.Annotate in your system privacy settings, then try Capture again."),
            ScreenCaptureOutcome.RestartRequired => new CaptureOutcomeFeedback(
                "Restart required",
                "Screen recording access changed. Restart AA.Annotate, then try Capture again."),
            ScreenCaptureOutcome.DisplayDisconnected => new CaptureOutcomeFeedback(
                "Display disconnected",
                "Reconnect the selected display or choose another display, then try Capture again."),
            ScreenCaptureOutcome.Unavailable => new CaptureOutcomeFeedback(
                "Screen capture unavailable",
                "Screen capture is not available in this desktop session. Check screen-capture services and permissions, then try again."),
            ScreenCaptureOutcome.Failed => new CaptureOutcomeFeedback(
                "Screen capture failed",
                "Try Capture again. If it keeps failing, restart AA.Annotate and verify screen-recording permission."),
            _ => new CaptureOutcomeFeedback(
                "Screen capture failed",
                "Try Capture again. If it keeps failing, restart AA.Annotate.")
        };

        if (feedback is null ||
            string.IsNullOrWhiteSpace(safeDetail) ||
            outcome == ScreenCaptureOutcome.Failed)
        {
            return feedback;
        }

        return feedback with
        {
            Message = $"{feedback.Message}{Environment.NewLine}{Environment.NewLine}{safeDetail.Trim()}"
        };
    }
}
