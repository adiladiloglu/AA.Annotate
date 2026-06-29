using System.Text.Json;
using AA.Annotate.Core.Models;
using AA.Annotate.Core.Serialization;

namespace AA.Annotate.Core.Services;

public sealed class SessionStore
{
    private readonly Func<DateTimeOffset> _clock;
    private readonly JsonSerializerOptions _jsonOptions;

    public SessionStore(Func<DateTimeOffset>? clock = null)
    {
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _jsonOptions = SessionJsonOptions.Create();
    }

    public async Task<SessionPaths> CreateSessionAsync(string? outputFolder, CancellationToken cancellationToken = default)
    {
        var now = _clock();
        var sessionId = CreateSessionId(now);
        var root = string.IsNullOrWhiteSpace(outputFolder)
            ? Path.Combine(Path.GetTempPath(), "AA.Annotate", "sessions")
            : outputFolder;
        var sessionFolder = Path.Combine(root, sessionId);
        Directory.CreateDirectory(sessionFolder);
        Directory.CreateDirectory(Path.Combine(sessionFolder, "captures"));

        var paths = SessionPaths.FromFolder(sessionFolder);
        var status = new SessionStatusDocument(
            SessionStatus.Waiting,
            sessionId,
            now,
            CompletedAtUtc: null,
            CancelledAtUtc: null,
            ReviewPath: null,
            AnnotationsPath: null,
            ErrorMessage: null)
        {
            LastActivityAtUtc = now
        };

        await WriteJsonAtomicAsync(paths.StatusJsonPath, status, cancellationToken);
        return paths;
    }

    public async Task MarkCompletedAsync(
        SessionPaths paths,
        string reviewPath,
        string annotationsPath,
        CancellationToken cancellationToken = default)
    {
        var current = await ReadStatusAsync(paths, cancellationToken);
        var completed = current with
        {
            Status = SessionStatus.Completed,
            CompletedAtUtc = _clock(),
            ReviewPath = reviewPath,
            AnnotationsPath = annotationsPath,
            ErrorMessage = null
        };
        await WriteJsonAtomicAsync(paths.StatusJsonPath, completed, cancellationToken);
    }

    public async Task MarkCancelledAsync(SessionPaths paths, CancellationToken cancellationToken = default)
    {
        var current = await ReadStatusAsync(paths, cancellationToken);
        var cancelled = current with
        {
            Status = SessionStatus.Cancelled,
            CancelledAtUtc = _clock()
        };
        await WriteJsonAtomicAsync(paths.StatusJsonPath, cancelled, cancellationToken);
    }

    public async Task MarkErrorAsync(
        SessionPaths paths,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        var current = await ReadStatusAsync(paths, cancellationToken);
        var error = current with
        {
            Status = SessionStatus.Error,
            ErrorMessage = errorMessage
        };
        await WriteJsonAtomicAsync(paths.StatusJsonPath, error, cancellationToken);
    }

    public async Task TouchActivityAsync(SessionPaths paths, CancellationToken cancellationToken = default)
    {
        var current = await ReadStatusAsync(paths, cancellationToken);
        if (current.Status is SessionStatus.Completed or SessionStatus.Cancelled or SessionStatus.Error)
        {
            return;
        }

        var active = current with
        {
            LastActivityAtUtc = _clock()
        };
        await WriteJsonAtomicAsync(paths.StatusJsonPath, active, cancellationToken);
    }

    public async Task<SessionStatusDocument> ReadStatusAsync(SessionPaths paths, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(paths.StatusJsonPath);
        var status = await JsonSerializer.DeserializeAsync<SessionStatusDocument>(stream, _jsonOptions, cancellationToken);
        return status ?? throw new InvalidDataException($"Session status file is empty: {paths.StatusJsonPath}");
    }

    private async Task WriteJsonAtomicAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        var tempPath = path + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, value, _jsonOptions, cancellationToken);
        }

        File.Move(tempPath, path, overwrite: true);
    }

    private static string CreateSessionId(DateTimeOffset now)
    {
        return $"{now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}"[..31];
    }
}
