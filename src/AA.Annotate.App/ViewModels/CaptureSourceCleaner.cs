namespace AA.Annotate.App.ViewModels;

internal static class CaptureSourceCleaner
{
    public static void DeleteRawSources(IEnumerable<CaptureViewModel> captures)
    {
        foreach (var capture in captures)
        {
            if (CaptureSourceCleanupPolicy.ShouldDeleteRawSource(capture))
            {
                TryDeleteFile(capture.ScreenshotPath);
                TryDeleteFile(capture.ThumbnailPath);
            }

            if (capture.PreviewPath is { } previewPath)
            {
                TryDeleteFile(previewPath);
            }
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
