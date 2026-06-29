namespace AA.Annotate.Core.Models;

public sealed record SessionStatusDocument(
    SessionStatus Status,
    string SessionId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string? ReviewPath,
    string? AnnotationsPath,
    string? ErrorMessage);
