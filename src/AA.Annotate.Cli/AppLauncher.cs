using System.Diagnostics;

namespace AA.Annotate.Cli;

public class AppLauncher
{
    public virtual Process Launch(string sessionFolder, TimeSpan? idleTimeout = null)
    {
        var executable = ResolveExecutablePath();
        var startInfo = CreateStartInfo(executable, sessionFolder, idleTimeout);
        return Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start {executable}.");
    }

    internal static ProcessStartInfo CreateStartInfo(string executable, string sessionFolder, TimeSpan? idleTimeout)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = true
        };
        startInfo.ArgumentList.Add("--session");
        startInfo.ArgumentList.Add(sessionFolder);
        if (idleTimeout is { } timeout)
        {
            startInfo.ArgumentList.Add("--idle-timeout-seconds");
            startInfo.ArgumentList.Add(timeout.TotalSeconds.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
        }

        return startInfo;
    }

    public virtual string ResolveExecutablePath()
    {
        return ResolveExecutable(AppContext.BaseDirectory, Environment.GetEnvironmentVariable);
    }

    internal static string ResolveExecutable(string baseDirectory, Func<string, string?> getEnvironmentVariable)
    {
        var overridePath = getEnvironmentVariable("AA_ANNOTATE_APP");
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            if (!File.Exists(overridePath))
            {
                throw new FileNotFoundException(
                    $"AA_ANNOTATE_APP points to a file that does not exist: {overridePath}",
                    overridePath);
            }

            return overridePath;
        }

        var adjacentApp = Path.GetFullPath(Path.Combine(baseDirectory, "..", "app", "AA.Annotate.App.exe"));
        if (File.Exists(adjacentApp))
        {
            return adjacentApp;
        }

        var adjacentRuntimePublishedApp = Path.GetFullPath(Path.Combine(
            baseDirectory,
            "..",
            "app-win-x64",
            "AA.Annotate.App.exe"));
        if (File.Exists(adjacentRuntimePublishedApp))
        {
            return adjacentRuntimePublishedApp;
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
