using AA.Annotate.App.Services;

namespace AA.Annotate.App.ViewModels;

internal sealed record SessionConfirmationPresentation(
    string Title,
    string Message,
    string ConfirmText,
    bool IsDestructive);

internal static class SessionConfirmationPolicy
{
    public static SessionConfirmationPresentation CreateFinish(LaunchCaller caller)
    {
        return caller == LaunchCaller.Agent
            ? new SessionConfirmationPresentation(
                "Finish annotation session?",
                "This will send the current captures and annotations to the agent. You cannot add more annotations after finishing.",
                "Send",
                IsDestructive: false)
            : new SessionConfirmationPresentation(
                "Finish annotation session?",
                "This will export the current captures and annotations. You cannot add more annotations after finishing.",
                "Export",
                IsDestructive: false);
    }

    public static SessionConfirmationPresentation CreateCancel()
    {
        return new SessionConfirmationPresentation(
            "Cancel annotation session?",
            "This will discard the current captures and annotations and close AA Annotate. This cannot be undone.",
            "Cancel session",
            IsDestructive: true);
    }
}
