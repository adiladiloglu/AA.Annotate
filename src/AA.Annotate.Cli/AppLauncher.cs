using System.Diagnostics;

namespace AA.Annotate.Cli;

public sealed class AppLauncher
{
    public Process Launch(string sessionFolder)
    {
        var executable = ResolveExecutable();
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("--session");
        startInfo.ArgumentList.Add(sessionFolder);
        return Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start {executable}.");
    }

    private static string ResolveExecutable()
    {
        return ResolveExecutable(AppContext.BaseDirectory, Environment.GetEnvironmentVariable);
    }

    internal static string ResolveExecutable(string baseDirectory, Func<string, string?> getEnvironmentVariable)
    {
        var overridePath = getEnvironmentVariable("AA_ANNOTATE_APP");
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return overridePath;
        }

        var adjacentApp = Path.GetFullPath(Path.Combine(baseDirectory, "..", "app", "AA.Annotate.App.exe"));
        if (File.Exists(adjacentApp))
        {
            return adjacentApp;
        }

        var sameDirectoryApp = Path.Combine(baseDirectory, "AA.Annotate.App.exe");
        if (File.Exists(sameDirectoryApp))
        {
            return sameDirectoryApp;
        }

        var appProject = Path.GetFullPath(Path.Combine(
            baseDirectory,
            "..",
            "..",
            "..",
            "..",
            "AA.Annotate.App"));
        var appBuild = Path.Combine(appProject, "bin", "Debug");
        var appFromBuild = Directory.Exists(appBuild)
            ? Directory.EnumerateFiles(appBuild, "AA.Annotate.App.exe", SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault()
            : null;

        return !string.IsNullOrWhiteSpace(appFromBuild) && File.Exists(appFromBuild)
            ? appFromBuild
            : "AA.Annotate.App";
    }
}
