using AA.Annotate.Core.Models;

namespace AA.Annotate.Core.Services;

public interface IAnnotationArtifactWriter
{
    Task<AnnotationCapture> WriteAsync(
        SessionPaths paths,
        AnnotationCapture capture,
        CancellationToken cancellationToken = default);
}
