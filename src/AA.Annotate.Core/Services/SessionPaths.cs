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

    public static SessionPaths FromFolder(string sessionFolder, string? exportFolder = null)
    {
        var resolvedExportFolder = string.IsNullOrWhiteSpace(exportFolder)
            ? CreateDefaultExportFolder(sessionFolder)
            : exportFolder;
        return new SessionPaths(
            sessionFolder,
            resolvedExportFolder,
            Path.Combine(sessionFolder, "status.json"),
            Path.Combine(resolvedExportFolder, "annotations.json"),
            Path.Combine(resolvedExportFolder, "review.md"));
    }

    private static string CreateDefaultExportFolder(string sessionFolder)
    {
        var sessionId = Path.GetFileName(sessionFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var exportFolder = Path.Combine(Path.GetTempPath(), "AA.Annotate", "exports", sessionId);
        return string.Equals(
            Path.GetFullPath(sessionFolder),
            Path.GetFullPath(exportFolder),
            StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(Path.GetTempPath(), "AA.Annotate", "exports", $"{sessionId}-export")
            : exportFolder;
    }
}
