using System.Text.Json;
using AA.Annotate.Core.Models;
using AA.Annotate.Core.Serialization;

namespace AA.Annotate.Core.Services;

public sealed class SessionExporter
{
    private readonly JsonSerializerOptions _jsonOptions = SessionJsonOptions.Create();

    public async Task ExportAsync(SessionPaths paths, AnnotationSession session, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(paths.SessionFolder);
        await File.WriteAllTextAsync(paths.ReviewMarkdownPath, ReviewMarkdownWriter.Write(session), cancellationToken);

        await using var stream = File.Create(paths.AnnotationsJsonPath);
        await JsonSerializer.SerializeAsync(stream, session, _jsonOptions, cancellationToken);
    }
}
