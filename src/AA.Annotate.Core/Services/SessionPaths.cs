namespace AA.Annotate.Core.Services;

public sealed record SessionPaths(
    string SessionFolder,
    string ExportFolder,
    string StatusJsonPath,
    string AnnotationsJsonPath,
    string ReviewMarkdownPath)
{
    public string WorkingCapturesFolder => Path.Combine(SessionFolder, "captures");

    public string ExportCapturesFolder => Path.Combine(ExportFolder, "captures");

    public string CapturesFolder => ExportCapturesFolder;

    public static SessionPaths FromFolder(
        string sessionFolder,
        string? exportFolder = null,
        AppPathResolver? pathResolver = null)
    {
        var resolvedExportFolder = string.IsNullOrWhiteSpace(exportFolder)
            ? CreateDefaultExportFolder(
                sessionFolder,
                pathResolver ?? new AppPathResolver())
            : exportFolder;

        if (PathsReferToSameLocation(sessionFolder, resolvedExportFolder))
        {
            throw new ArgumentException(
                "The export folder must be different from the private session folder.",
                nameof(exportFolder));
        }

        return new SessionPaths(
            sessionFolder,
            resolvedExportFolder,
            Path.Combine(sessionFolder, "status.json"),
            Path.Combine(resolvedExportFolder, "annotations.json"),
            Path.Combine(resolvedExportFolder, "review.md"));
    }

    private static string CreateDefaultExportFolder(
        string sessionFolder,
        AppPathResolver pathResolver)
    {
        var sessionId = Path.GetFileName(sessionFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var exportFolder = Path.Combine(
            pathResolver.GetPrivateRuntimeDirectory(),
            "exports",
            sessionId);
        return PathsReferToSameLocation(sessionFolder, exportFolder)
            ? Path.Combine(
                pathResolver.GetPrivateRuntimeDirectory(),
                "exports",
                $"{sessionId}-export")
            : exportFolder;
    }

    private static bool PathsReferToSameLocation(string firstPath, string secondPath)
    {
        var first = ResolveComparablePath(firstPath);
        var second = ResolveComparablePath(secondPath);
        var comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        return string.Equals(first, second, comparison);
    }

    private static string ResolveComparablePath(string path)
    {
        var fullPath = Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        try
        {
            return Directory.Exists(fullPath)
                ? (Directory.ResolveLinkTarget(fullPath, returnFinalTarget: true)?.FullName ?? fullPath)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                : fullPath;
        }
        catch (IOException)
        {
            return fullPath;
        }
        catch (UnauthorizedAccessException)
        {
            return fullPath;
        }
    }
}
