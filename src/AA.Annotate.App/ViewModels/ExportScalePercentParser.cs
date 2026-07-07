namespace AA.Annotate.App.ViewModels;

public static class ExportScalePercentParser
{
    public static int ParseOrDefault(string? text, int fallbackPercent)
    {
        var value = text?.Trim().TrimEnd('%');
        if (!int.TryParse(value, out var percent))
        {
            return Clamp(fallbackPercent);
        }

        return Clamp(percent);
    }

    public static int Clamp(int percent)
    {
        return Math.Clamp(percent, 20, 100);
    }
}
