using System.Text.Json;
using AA.Annotate.App.Services;
using AA.Annotate.App.ViewModels;
using Avalonia;

namespace AA.Annotate.App.Tests;

public sealed class UiSettingsStoreTests
{
    [Fact]
    public async Task MissingSettingsReturnDefaults()
    {
        using var directory = new TemporaryDirectory();
        var store = new UiSettingsStore(Path.Combine(directory.Path, "missing.json"));

        var settings = await store.LoadAsync();

        Assert.Equal(UiSettings.CurrentSchemaVersion, settings.SchemaVersion);
        Assert.Null(settings.Toolbar);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{not-json")]
    [InlineData("null")]
    public async Task EmptyOrCorruptSettingsReturnDefaults(string json)
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "ui-settings.json");
        await File.WriteAllTextAsync(path, json);
        var store = new UiSettingsStore(path);

        var settings = await store.LoadAsync();

        Assert.Equal(new UiSettings(), settings);
    }

    [Fact]
    public async Task UnknownSchemaReturnsDefaults()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "ui-settings.json");
        await File.WriteAllTextAsync(path, """
            {
              "schemaVersion": 99,
              "toolbar": {
                "displayName": "future"
              }
            }
            """);
        var store = new UiSettingsStore(path);

        var settings = await store.LoadAsync();

        Assert.Equal(new UiSettings(), settings);
    }

    [Fact]
    public async Task SaveAndLoadRoundTripPlacement()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "nested", "ui-settings.json");
        var store = new UiSettingsStore(path);
        var expected = new UiSettings
        {
            Toolbar = new ToolbarPlacement(
                "display-2",
                new PixelRect(-2560, 0, 2560, 1440),
                0.375,
                0.625)
        };

        var saved = await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        Assert.True(saved);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task SaveStampsCurrentSchemaVersion()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "ui-settings.json");
        var store = new UiSettingsStore(path);

        var saved = await store.SaveAsync(new UiSettings { SchemaVersion = -1 });
        var json = await File.ReadAllTextAsync(path);
        using var document = JsonDocument.Parse(json);

        Assert.True(saved);
        Assert.Equal(
            UiSettings.CurrentSchemaVersion,
            document.RootElement.GetProperty("schemaVersion").GetInt32());
    }

    [Fact]
    public async Task SaveAtomicallyOverwritesExistingSettings()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "ui-settings.json");
        var store = new UiSettingsStore(path);
        await store.SaveAsync(new UiSettings
        {
            Toolbar = new ToolbarPlacement(
                "old",
                new PixelRect(0, 0, 1920, 1080),
                0,
                0)
        });
        var replacement = new UiSettings
        {
            Toolbar = new ToolbarPlacement(
                "new",
                new PixelRect(1920, 0, 2560, 1440),
                1,
                1)
        };

        var saved = await store.SaveAsync(replacement);
        var actual = await store.LoadAsync();

        Assert.True(saved);
        Assert.Equal(replacement, actual);
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp"));
    }

    [Fact]
    public async Task ConcurrentSavesUseIndependentTemporaryFiles()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "ui-settings.json");
        var firstStore = new UiSettingsStore(path);
        var secondStore = new UiSettingsStore(path);
        var first = new UiSettings
        {
            Toolbar = new ToolbarPlacement(
                "first",
                new PixelRect(0, 0, 1920, 1080),
                0.25,
                0.25)
        };
        var second = new UiSettings
        {
            Toolbar = new ToolbarPlacement(
                "second",
                new PixelRect(1920, 0, 2560, 1440),
                0.75,
                0.75)
        };

        var results = await Task.WhenAll(
            firstStore.SaveAsync(first),
            secondStore.SaveAsync(second));
        var actual = await firstStore.LoadAsync();

        Assert.All(results, Assert.True);
        Assert.True(actual == first || actual == second);
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp"));
    }

    [Fact]
    public async Task InaccessibleSaveDoesNotThrow()
    {
        using var directory = new TemporaryDirectory();
        var blockingFile = Path.Combine(directory.Path, "not-a-directory");
        await File.WriteAllTextAsync(blockingFile, "content");
        var store = new UiSettingsStore(
            Path.Combine(blockingFile, "ui-settings.json"));

        var saved = await store.SaveAsync(new UiSettings());

        Assert.False(saved);
    }

    [Fact]
    public async Task DirectoryAtSettingsPathDoesNotBlockLoad()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "ui-settings.json");
        Directory.CreateDirectory(path);
        var store = new UiSettingsStore(path);

        var settings = await store.LoadAsync();

        Assert.Equal(new UiSettings(), settings);
    }

    [Fact]
    public async Task CancellationIsPropagated()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "ui-settings.json");
        var store = new UiSettingsStore(path);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => store.SaveAsync(new UiSettings(), cancellation.Token));
    }

    [Fact]
    public void DefaultPathUsesLocalApplicationData()
    {
        var expectedRoot = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);

        var path = UiSettingsStore.GetDefaultSettingsPath();

        Assert.StartsWith(expectedRoot, path, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(
            Path.Combine("AA.Annotate", "ui-settings.json"),
            path,
            StringComparison.OrdinalIgnoreCase);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "AA.Annotate.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch
            {
                // Test cleanup must not obscure the assertion result.
            }
        }
    }
}
