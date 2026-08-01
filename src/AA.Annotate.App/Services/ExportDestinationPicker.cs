using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace AA.Annotate.App.Services;

internal interface IExportDestinationPicker
{
    Task<string?> PickAsync(Window owner);
}

internal sealed class ExportDestinationPicker : IExportDestinationPicker
{
    public async Task<string?> PickAsync(Window owner)
    {
        var folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose AA.Annotate export destination",
            AllowMultiple = false
        });
        return folders.Count == 0 ? null : folders[0].TryGetLocalPath();
    }
}
