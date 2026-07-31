using System.Text.Json;
using AA.Annotate.Core.Serialization;

namespace AA.Annotate.App.Services;

public sealed class UiSettingsStore
{
    private readonly JsonSerializerOptions _jsonOptions;

    public UiSettingsStore(string? settingsPath = null)
    {
        SettingsPath = string.IsNullOrWhiteSpace(settingsPath)
            ? GetDefaultSettingsPath()
            : Path.GetFullPath(settingsPath);
        _jsonOptions = SessionJsonOptions.Create();
    }

    public string SettingsPath { get; }

    public async Task<UiSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var stream = new FileStream(
                SettingsPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);
            var settings = await JsonSerializer.DeserializeAsync<UiSettings>(
                stream,
                _jsonOptions,
                cancellationToken);

            return settings?.SchemaVersion == UiSettings.CurrentSchemaVersion
                ? settings
                : new UiSettings();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsRecoverable(exception))
        {
            return new UiSettings();
        }
    }

    public async Task<bool> SaveAsync(
        UiSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var directory = Path.GetDirectoryName(SettingsPath);
        var tempPath = $"{SettingsPath}.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp";

        try
        {
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var currentSettings = settings with
            {
                SchemaVersion = UiSettings.CurrentSchemaVersion
            };
            await using (var stream = new FileStream(
                             tempPath,
                             FileMode.Create,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 4096,
                             useAsync: true))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    currentSettings,
                    _jsonOptions,
                    cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(tempPath, SettingsPath, overwrite: true);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TryDelete(tempPath);
            throw;
        }
        catch (Exception exception) when (IsRecoverable(exception))
        {
            TryDelete(tempPath);
            return false;
        }
    }

    public static string GetDefaultSettingsPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AA.Annotate",
            "ui-settings.json");
    }

    private static bool IsRecoverable(Exception exception)
    {
        return exception is IOException
            or UnauthorizedAccessException
            or JsonException
            or NotSupportedException;
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
            // Settings persistence is best effort and must not prevent shutdown.
        }
    }
}
