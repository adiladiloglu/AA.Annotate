using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AA.Annotate.Cli;

internal enum AppHostPlatform
{
    Windows,
    Linux,
    MacOS
}

public class AppLauncher
{
    public virtual Process Launch(string sessionFolder, string exportFolder, TimeSpan? idleTimeout = null)
    {
        var executable = ResolveExecutablePath();
        var startInfo = CreateStartInfo(executable, sessionFolder, exportFolder, idleTimeout);
        return Process.Start(startInfo) ?? throw new InvalidOperationException($"Failed to start {executable}.");
    }

    internal static ProcessStartInfo CreateStartInfo(string executable, string sessionFolder, string exportFolder, TimeSpan? idleTimeout)
    {
        return CreateStartInfo(executable, sessionFolder, exportFolder, idleTimeout, GetCurrentPlatform());
    }

    internal static ProcessStartInfo CreateStartInfo(
        string executable,
        string sessionFolder,
        string exportFolder,
        TimeSpan? idleTimeout,
        AppHostPlatform platform)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            // Preserve the Windows launch behavior while directly executing the
            // Unix app host so Process.Id remains the process SessionWaiter owns.
            UseShellExecute = platform == AppHostPlatform.Windows
        };
        startInfo.ArgumentList.Add("--session");
        startInfo.ArgumentList.Add(sessionFolder);
        startInfo.ArgumentList.Add("--export");
        startInfo.ArgumentList.Add(exportFolder);
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
        return ResolveExecutable(
            baseDirectory,
            getEnvironmentVariable,
            GetCurrentPlatform(),
            RuntimeInformation.ProcessArchitecture);
    }

    internal static string ResolveExecutable(
        string baseDirectory,
        Func<string, string?> getEnvironmentVariable,
        AppHostPlatform platform,
        Architecture architecture)
    {
        var overridePath = getEnvironmentVariable("AA_ANNOTATE_APP");
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            var overrideExecutable = ResolveOverride(overridePath, platform);
            if (overrideExecutable is null)
            {
                throw new FileNotFoundException(
                    $"AA_ANNOTATE_APP points to an app executable or bundle that does not exist: {overridePath}",
                    overridePath);
            }

            return overrideExecutable;
        }

        foreach (var relativeExecutable in GetExecutableRelativePaths(platform))
        {
            var adjacentApp = Path.GetFullPath(Path.Combine(baseDirectory, "..", "app", relativeExecutable));
            if (File.Exists(adjacentApp))
            {
                return adjacentApp;
            }
        }

        var runtimeFolder = $"app-{GetRuntimeIdentifier(platform, architecture)}";
        foreach (var relativeExecutable in GetExecutableRelativePaths(platform))
        {
            var adjacentRuntimePublishedApp = Path.GetFullPath(Path.Combine(
                baseDirectory,
                "..",
                runtimeFolder,
                relativeExecutable));
            if (File.Exists(adjacentRuntimePublishedApp))
            {
                return adjacentRuntimePublishedApp;
            }
        }

        foreach (var relativeExecutable in GetExecutableRelativePaths(platform))
        {
            var sameDirectoryApp = Path.Combine(baseDirectory, relativeExecutable);
            if (File.Exists(sameDirectoryApp))
            {
                return sameDirectoryApp;
            }
        }

        var appProject = Path.GetFullPath(Path.Combine(
            baseDirectory,
            "..",
            "..",
            "..",
            "..",
            "AA.Annotate.App"));
        var appBuild = Path.Combine(appProject, "bin", "Debug");
        var appFromBuild = FindNewestBuildExecutable(appBuild, platform, architecture);

        return !string.IsNullOrWhiteSpace(appFromBuild) && File.Exists(appFromBuild)
            ? appFromBuild
            : "AA.Annotate.App";
    }

    private static string? ResolveOverride(string overridePath, AppHostPlatform platform)
    {
        if (File.Exists(overridePath))
        {
            return overridePath;
        }

        if (platform != AppHostPlatform.MacOS || !Directory.Exists(overridePath))
        {
            return null;
        }

        var bundleExecutable = Path.Combine(overridePath, "Contents", "MacOS", "AA.Annotate.App");
        return File.Exists(bundleExecutable) ? bundleExecutable : null;
    }

    internal static string? FindNewestBuildExecutable(
        string appBuild,
        AppHostPlatform platform,
        Architecture architecture)
    {
        if (!Directory.Exists(appBuild))
        {
            return null;
        }

        var expectedRid = GetRuntimeIdentifier(platform, architecture);
        return GetExecutableFileNames(platform)
            .SelectMany(fileName => Directory.EnumerateFiles(appBuild, fileName, SearchOption.AllDirectories))
            .Where(path => IsCompatibleBuildPath(appBuild, path, expectedRid))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static bool IsCompatibleBuildPath(string appBuild, string executablePath, string expectedRid)
    {
        var segments = Path.GetRelativePath(appBuild, executablePath)
            .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
        var ridSegments = segments.Where(segment =>
            segment.StartsWith("win-", StringComparison.OrdinalIgnoreCase) ||
            segment.StartsWith("linux-", StringComparison.OrdinalIgnoreCase) ||
            segment.StartsWith("osx-", StringComparison.OrdinalIgnoreCase));

        return !ridSegments.Any() ||
            ridSegments.Contains(expectedRid, StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> GetExecutableRelativePaths(AppHostPlatform platform)
    {
        return platform switch
        {
            AppHostPlatform.Windows => ["AA.Annotate.App.exe"],
            AppHostPlatform.Linux => ["AA.Annotate.App"],
            AppHostPlatform.MacOS =>
            [
                Path.Combine("AA.Annotate.App.app", "Contents", "MacOS", "AA.Annotate.App"),
                "AA.Annotate.App"
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
        };
    }

    private static IReadOnlyList<string> GetExecutableFileNames(AppHostPlatform platform)
    {
        return platform == AppHostPlatform.Windows
            ? ["AA.Annotate.App.exe"]
            : ["AA.Annotate.App"];
    }

    private static string GetRuntimeIdentifier(AppHostPlatform platform, Architecture architecture)
    {
        var os = platform switch
        {
            AppHostPlatform.Windows => "win",
            AppHostPlatform.Linux => "linux",
            AppHostPlatform.MacOS => "osx",
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
        };
        var architectureName = architecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            _ => architecture.ToString().ToLowerInvariant()
        };
        return $"{os}-{architectureName}";
    }

    private static AppHostPlatform GetCurrentPlatform()
    {
        if (OperatingSystem.IsWindows())
        {
            return AppHostPlatform.Windows;
        }

        if (OperatingSystem.IsLinux())
        {
            return AppHostPlatform.Linux;
        }

        if (OperatingSystem.IsMacOS())
        {
            return AppHostPlatform.MacOS;
        }

        throw new PlatformNotSupportedException("AA Annotate supports Windows, Linux, and macOS.");
    }
}
