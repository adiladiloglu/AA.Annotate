namespace AA.Annotate.Core.Models;

public static class CaptureScale
{
    public const int MinimumPercent = 20;
    public const int MaximumPercent = 100;
    public const int DefaultPercent = 100;

    public static int Clamp(int percent) => Math.Clamp(percent, MinimumPercent, MaximumPercent);

    public static int ParseOrDefault(string? text, int fallbackPercent = DefaultPercent)
    {
        var value = text?.Trim().TrimEnd('%');
        return int.TryParse(value, out var percent) ? Clamp(percent) : Clamp(fallbackPercent);
    }
}
