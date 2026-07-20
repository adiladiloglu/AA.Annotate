using AA.Annotate.App.ViewModels;
using AA.Annotate.Platform;

namespace AA.Annotate.App.Tests;

public sealed class CaptureOutcomeFeedbackPolicyTests
{
    [Theory]
    [InlineData(ScreenCaptureOutcome.Completed)]
    [InlineData(ScreenCaptureOutcome.Cancelled)]
    public void SuccessfulOrCancelledCaptureDoesNotShowFeedback(ScreenCaptureOutcome outcome)
    {
        Assert.Null(CaptureOutcomeFeedbackPolicy.Create(outcome));
    }

    [Theory]
    [InlineData(ScreenCaptureOutcome.PermissionDenied, "permission", "privacy settings")]
    [InlineData(ScreenCaptureOutcome.RestartRequired, "Restart", "Restart")]
    [InlineData(ScreenCaptureOutcome.DisplayDisconnected, "disconnected", "choose another display")]
    [InlineData(ScreenCaptureOutcome.Unavailable, "unavailable", "services and permissions")]
    [InlineData(ScreenCaptureOutcome.Failed, "failed", "Try Capture again")]
    public void RecoverableFailureShowsActionableFeedback(
        ScreenCaptureOutcome outcome,
        string expectedTitleText,
        string expectedActionText)
    {
        var feedback = CaptureOutcomeFeedbackPolicy.Create(outcome);

        Assert.NotNull(feedback);
        Assert.Contains(expectedTitleText, feedback.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(expectedActionText, feedback.Message, StringComparison.OrdinalIgnoreCase);
    }
}
