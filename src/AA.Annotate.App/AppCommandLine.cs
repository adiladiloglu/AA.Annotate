namespace AA.Annotate.App;

internal static class AppCommandLine
{
    public const string HelpText = """
        Usage:
          AA.Annotate.App.exe [--session <folder>] [--session-root <folder>] [--idle-timeout-seconds <seconds>]
          AA.Annotate.App.exe --help

        Options:
          --session <folder>                  Use an existing session folder created by the CLI.
          --session-root <folder>             Create a new session under this root folder.
          --idle-timeout-seconds <seconds>    Inactivity timeout before the app warns and closes.
          -h, --help, /?                      Show this help.
        """;

    public static bool IsHelpRequested(IReadOnlyList<string> args)
    {
        return args.Any(arg =>
            string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(arg, "/?", StringComparison.OrdinalIgnoreCase));
    }
}
