namespace AA.Annotate.Core.Services;

public sealed record SessionPaths(
    string SessionFolder,
    string StatusJsonPath,
    string AnnotationsJsonPath,
    string ReviewMarkdownPath)
{
    public string CapturesFolder => Path.Combine(SessionFolder, "captures");

    public static SessionPaths FromFolder(string sessionFolder)
    {
        return new SessionPaths(
            sessionFolder,
            Path.Combine(sessionFolder, "status.json"),
            Path.Combine(sessionFolder, "annotations.json"),
            Path.Combine(sessionFolder, "review.md"));
    }
}
