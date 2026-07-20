namespace AA.Annotate.App;

internal static class AppCommandLine
{
    public const string HelpText = """
        Usage:
          AA.Annotate.App [--session <folder>] [--export <folder>] [--session-root <folder>] [--export-root <folder>] [--idle-timeout-seconds <seconds>]
          AA.Annotate.App --help

        Options:
          --session <folder>                  Use an existing session folder created by the CLI.
          --export <folder>                   Write final review and exported images to this folder.
          --session-root <folder>             Create a new session under this root folder.
          --export-root <folder>              Create a final export folder under this root folder.
          --idle-timeout-seconds <seconds>    Inactivity timeout before the app warns and closes. Default: 60.
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
