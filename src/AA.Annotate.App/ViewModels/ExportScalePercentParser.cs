using AA.Annotate.Core.Models;

namespace AA.Annotate.App.ViewModels;

public static class ExportScalePercentParser
{
    public static int ParseOrDefault(string? text, int fallbackPercent)
    {
        return CaptureScale.ParseOrDefault(text, fallbackPercent);
    }

    public static int Clamp(int percent)
    {
        return CaptureScale.Clamp(percent);
    }
}
