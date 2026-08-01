using System.Text.Json;
using AA.Annotate.Core.Models;
using AA.Annotate.Core.Serialization;

namespace AA.Annotate.Core.Services;

public sealed class SessionStore
{
    private readonly Func<DateTimeOffset> _clock;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly AppPathResolver _pathResolver;
    private readonly SemaphoreSlim _statusMutationGate = new(1, 1);

    public SessionStore(
        Func<DateTimeOffset>? clock = null,
        AppPathResolver? pathResolver = null)
    {
        _clock = clock ?? (() => DateTimeOffset.UtcNow);
        _pathResolver = pathResolver ?? new AppPathResolver();
        _jsonOptions = SessionJsonOptions.Create();
    }

    public async Task<SessionPaths> CreateSessionAsync(
        string? sessionRoot,
        string? exportRoot = null,
        CancellationToken cancellationToken = default)
    {
        var now = _clock();
        var sessionId = CreateSessionId(now);
        var root = string.IsNullOrWhiteSpace(sessionRoot)
            ? Path.Combine(_pathResolver.GetPrivateRuntimeDirectory(), "sessions")
            : sessionRoot;
        var resolvedExportRoot = string.IsNullOrWhiteSpace(exportRoot)
            ? Path.Combine(_pathResolver.GetPrivateRuntimeDirectory(), "exports")
            : exportRoot;
        var sessionFolder = Path.Combine(root, sessionId);
        var exportFolder = Path.Combine(resolvedExportRoot, sessionId);
        PrivateFileSystem.CreateDirectory(root);
        PrivateFileSystem.CreateDirectory(sessionFolder);
        PrivateFileSystem.CreateDirectory(Path.Combine(sessionFolder, "captures"));
        Directory.CreateDirectory(exportFolder);
        Directory.CreateDirectory(Path.Combine(exportFolder, "captures"));

        var paths = SessionPaths.FromFolder(sessionFolder, exportFolder, _pathResolver);
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
        await MutateStatusAsync(
            paths,
            current => current with
            {
                Status = SessionStatus.Completed,
                CompletedAtUtc = _clock(),
                ReviewPath = reviewPath,
                AnnotationsPath = annotationsPath,
                ErrorMessage = null
            },
            cancellationToken);
    }

    public async Task MarkCancelledAsync(SessionPaths paths, CancellationToken cancellationToken = default)
    {
        await MutateStatusAsync(
            paths,
            current => current with
            {
                Status = SessionStatus.Cancelled,
                CancelledAtUtc = _clock()
            },
            cancellationToken);
    }

    public async Task MarkErrorAsync(
        SessionPaths paths,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        await MutateStatusAsync(
            paths,
            current => current with
            {
                Status = SessionStatus.Error,
                ErrorMessage = errorMessage
            },
            cancellationToken);
    }

    public async Task TouchActivityAsync(SessionPaths paths, CancellationToken cancellationToken = default)
    {
        await MutateStatusAsync(
            paths,
            current => current.Status is SessionStatus.Completed or SessionStatus.Cancelled or SessionStatus.Error
                ? null
                : current with { LastActivityAtUtc = _clock() },
            cancellationToken);
    }

    public async Task<SessionStatusDocument> ReadStatusAsync(SessionPaths paths, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(
            paths.StatusJsonPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);
        var status = await JsonSerializer.DeserializeAsync<SessionStatusDocument>(stream, _jsonOptions, cancellationToken);
        return status ?? throw new InvalidDataException($"Session status file is empty: {paths.StatusJsonPath}");
    }

    private async Task WriteJsonAtomicAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        var tempPath = path + ".tmp";
        await using (var stream = PrivateFileSystem.CreateFile(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, value, _jsonOptions, cancellationToken);
        }

        File.Move(tempPath, path, overwrite: true);
        PrivateFileSystem.ProtectFile(path);
    }

    private async Task MutateStatusAsync(
        SessionPaths paths,
        Func<SessionStatusDocument, SessionStatusDocument?> mutation,
        CancellationToken cancellationToken)
    {
        await _statusMutationGate.WaitAsync(cancellationToken);
        try
        {
            var current = await ReadStatusAsync(paths, cancellationToken);
            var updated = mutation(current);
            if (updated is not null)
            {
                await WriteJsonAtomicAsync(paths.StatusJsonPath, updated, cancellationToken);
            }
        }
        finally
        {
            _statusMutationGate.Release();
        }
    }

    private static string CreateSessionId(DateTimeOffset now)
    {
        return $"{now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}"[..31];
    }
}
