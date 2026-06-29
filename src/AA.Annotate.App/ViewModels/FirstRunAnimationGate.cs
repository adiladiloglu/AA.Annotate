namespace AA.Annotate.App.ViewModels;

internal static class FirstRunAnimationGate
{
    public static string DefaultMarkerPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AA.Annotate",
        "first-run-animation.seen");

    public static bool TryClaim(string? markerPath = null)
    {
        var path = markerPath ?? DefaultMarkerPath;
        try
        {
            if (File.Exists(path))
            {
                return false;
            }

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, DateTimeOffset.UtcNow.ToString("O"));
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
