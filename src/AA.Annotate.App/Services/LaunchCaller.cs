namespace AA.Annotate.App.Services;

public enum LaunchCaller
{
    Human,
    Agent
}

internal static class CompletionBehavior
{
    public static bool RequiresExportDestination(LaunchCaller caller) => caller == LaunchCaller.Human;
}
