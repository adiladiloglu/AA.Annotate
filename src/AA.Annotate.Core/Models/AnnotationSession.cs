namespace AA.Annotate.Core.Models;

public sealed record AnnotationSession(
    string SessionId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    SessionStatus Status,
    IReadOnlyList<AnnotationCapture> Captures);
