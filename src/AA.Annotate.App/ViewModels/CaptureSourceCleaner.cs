namespace AA.Annotate.App.ViewModels;

internal static class CaptureSourceCleaner
{
    public static void DeleteRawSources(IEnumerable<CaptureViewModel> captures, int exportScalePercent)
    {
        foreach (var capture in captures.Where(capture => CaptureSourceCleanupPolicy.ShouldDeleteRawSource(capture, exportScalePercent)))
        {
            TryDeleteFile(capture.ScreenshotPath);
            TryDeleteFile(capture.ThumbnailPath);
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
