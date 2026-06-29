using AA.Annotate.Core.Geometry;

namespace AA.Annotate.App.ViewModels;

public static class ViewportSizeSelector
{
    public static SizeInt Select(
        double configuredWidth,
        double configuredHeight,
        double boundsWidth,
        double boundsHeight,
        double canvasWidth,
        double canvasHeight,
        SizeInt fallback)
    {
        if (IsUsable(configuredWidth) && IsUsable(configuredHeight))
        {
            return ToSize(configuredWidth, configuredHeight);
        }

        if (IsUsable(boundsWidth) && IsUsable(boundsHeight))
        {
            return ToSize(boundsWidth, boundsHeight);
        }

        if (IsUsable(canvasWidth) && IsUsable(canvasHeight))
        {
            return ToSize(canvasWidth, canvasHeight);
        }

        return fallback;
    }

    private static SizeInt ToSize(double width, double height)
    {
        return new SizeInt(
            Math.Max(1, (int)Math.Round(width)),
            Math.Max(1, (int)Math.Round(height)));
    }

    private static bool IsUsable(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;
    }
}
