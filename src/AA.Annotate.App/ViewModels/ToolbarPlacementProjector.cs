using Avalonia;

namespace AA.Annotate.App.ViewModels;

public static class ToolbarPlacementProjector
{
    public const double DefaultMarginDips = 8;
    public const double DefaultInitialInsetDips = 24;

    public static ToolbarPlacement Project(
        PixelPoint position,
        Size toolbarSizeDips,
        IReadOnlyList<ToolbarDisplay> displays,
        double marginDips = DefaultMarginDips,
        double? toolbarScaling = null)
    {
        var display = FindDisplay(position, toolbarSizeDips, displays, toolbarScaling)
            ?? throw new ArgumentException("At least one display is required.", nameof(displays));
        var travel = GetTravelArea(display, toolbarSizeDips, marginDips);
        var clamped = ClampToTravelArea(position, travel);

        return new ToolbarPlacement(
            display.Name,
            display.Bounds,
            Normalize(clamped.X, travel.MinimumX, travel.MaximumX),
            Normalize(clamped.Y, travel.MinimumY, travel.MaximumY));
    }

    public static PixelPoint Restore(
        ToolbarPlacement? placement,
        Size toolbarSizeDips,
        IReadOnlyList<ToolbarDisplay> displays,
        double marginDips = DefaultMarginDips,
        double initialInsetDips = DefaultInitialInsetDips)
    {
        if (displays.Count == 0)
        {
            throw new ArgumentException("At least one display is required.", nameof(displays));
        }

        var display = placement is null
            ? SelectPrimaryDisplay(displays)
            : MatchDisplay(placement, displays);
        var travel = GetTravelArea(display, toolbarSizeDips, marginDips);

        if (placement is null)
        {
            var insetPixels = ToPhysicalPixels(initialInsetDips, display.Scaling);
            return ClampToTravelArea(
                new PixelPoint(
                    display.WorkingArea.X + insetPixels,
                    display.WorkingArea.Y + insetPixels),
                travel);
        }

        return new PixelPoint(
            Denormalize(placement.NormalizedX, travel.MinimumX, travel.MaximumX),
            Denormalize(placement.NormalizedY, travel.MinimumY, travel.MaximumY));
    }

    public static PixelPoint Clamp(
        PixelPoint position,
        Size toolbarSizeDips,
        ToolbarDisplay display,
        double marginDips = DefaultMarginDips)
    {
        return ClampToTravelArea(position, GetTravelArea(display, toolbarSizeDips, marginDips));
    }

    public static ToolbarDisplay? FindDisplay(
        PixelPoint position,
        Size toolbarSizeDips,
        IReadOnlyList<ToolbarDisplay> displays,
        double? toolbarScaling = null)
    {
        if (displays.Count == 0)
        {
            return null;
        }

        var currentScaling = toolbarScaling is { } supplied && IsPositiveFinite(supplied)
            ? supplied
            : displays.FirstOrDefault(display => display.WorkingArea.Contains(position))?.Scaling ?? 1;
        var toolbarWidth = ToPhysicalPixels(toolbarSizeDips.Width, currentScaling);
        var toolbarHeight = ToPhysicalPixels(toolbarSizeDips.Height, currentScaling);
        var toolbarBounds = new PixelRect(position.X, position.Y, toolbarWidth, toolbarHeight);
        ToolbarDisplay? bestDisplay = null;
        long bestOverlap = -1;
        double bestDistance = double.PositiveInfinity;

        foreach (var display in displays)
        {
            var overlap = IntersectionArea(toolbarBounds, display.WorkingArea);
            var distance = DistanceSquared(position, display.WorkingArea);

            if (overlap > bestOverlap
                || (overlap == bestOverlap && distance < bestDistance)
                || (overlap == bestOverlap
                    && distance.Equals(bestDistance)
                    && display.IsPrimary
                    && bestDisplay?.IsPrimary is not true))
            {
                bestDisplay = display;
                bestOverlap = overlap;
                bestDistance = distance;
            }
        }

        return bestDisplay;
    }

    private static ToolbarDisplay MatchDisplay(
        ToolbarPlacement placement,
        IReadOnlyList<ToolbarDisplay> displays)
    {
        var matchingNames = displays
            .Where(display => string.Equals(
                display.Name,
                placement.DisplayName,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matchingNames.Length == 1)
        {
            return matchingNames[0];
        }

        var candidates = matchingNames.Length > 1 ? matchingNames : displays.ToArray();
        var best = candidates
            .Select(display => new
            {
                Display = display,
                Overlap = IntersectionArea(placement.DisplayBounds, display.Bounds)
            })
            .OrderByDescending(candidate => candidate.Overlap)
            .ThenByDescending(candidate => candidate.Display.IsPrimary)
            .First();

        return best.Overlap > 0 || matchingNames.Length > 0
            ? best.Display
            : SelectPrimaryDisplay(displays);
    }

    private static ToolbarDisplay SelectPrimaryDisplay(IReadOnlyList<ToolbarDisplay> displays)
    {
        return displays.FirstOrDefault(display => display.IsPrimary) ?? displays[0];
    }

    private static TravelArea GetTravelArea(
        ToolbarDisplay display,
        Size toolbarSizeDips,
        double marginDips)
    {
        var scaling = IsPositiveFinite(display.Scaling) ? display.Scaling : 1;
        var margin = ToPhysicalPixels(Math.Max(0, SanitizeFinite(marginDips)), scaling);
        var toolbarWidth = ToPhysicalPixels(toolbarSizeDips.Width, scaling);
        var toolbarHeight = ToPhysicalPixels(toolbarSizeDips.Height, scaling);
        var minimumX = display.WorkingArea.X + margin;
        var minimumY = display.WorkingArea.Y + margin;

        return new TravelArea(
            minimumX,
            minimumY,
            Math.Max(minimumX, display.WorkingArea.Right - toolbarWidth - margin),
            Math.Max(minimumY, display.WorkingArea.Bottom - toolbarHeight - margin));
    }

    private static PixelPoint ClampToTravelArea(PixelPoint position, TravelArea travel)
    {
        return new PixelPoint(
            Math.Clamp(position.X, travel.MinimumX, travel.MaximumX),
            Math.Clamp(position.Y, travel.MinimumY, travel.MaximumY));
    }

    private static double Normalize(int value, int minimum, int maximum)
    {
        if (maximum <= minimum)
        {
            return 0;
        }

        return Math.Clamp((double)(value - minimum) / (maximum - minimum), 0, 1);
    }

    private static int Denormalize(double value, int minimum, int maximum)
    {
        if (maximum <= minimum)
        {
            return minimum;
        }

        var normalized = double.IsFinite(value) ? Math.Clamp(value, 0, 1) : 0;
        return minimum + (int)Math.Round(
            normalized * (maximum - minimum),
            MidpointRounding.AwayFromZero);
    }

    private static int ToPhysicalPixels(double dips, double scaling)
    {
        if (!IsPositiveFinite(dips))
        {
            return 0;
        }

        var safeScaling = IsPositiveFinite(scaling) ? scaling : 1;
        var pixels = Math.Ceiling(dips * safeScaling);
        return pixels >= int.MaxValue ? int.MaxValue : (int)pixels;
    }

    private static long IntersectionArea(PixelRect first, PixelRect second)
    {
        var width = Math.Max(0L, Math.Min((long)first.Right, second.Right) - Math.Max(first.X, second.X));
        var height = Math.Max(0L, Math.Min((long)first.Bottom, second.Bottom) - Math.Max(first.Y, second.Y));
        return width * height;
    }

    private static double DistanceSquared(PixelPoint point, PixelRect rectangle)
    {
        var closestX = Math.Clamp(point.X, rectangle.X, rectangle.Right);
        var closestY = Math.Clamp(point.Y, rectangle.Y, rectangle.Bottom);
        var deltaX = (double)point.X - closestX;
        var deltaY = (double)point.Y - closestY;
        return (deltaX * deltaX) + (deltaY * deltaY);
    }

    private static bool IsPositiveFinite(double value)
    {
        return double.IsFinite(value) && value > 0;
    }

    private static double SanitizeFinite(double value)
    {
        return double.IsFinite(value) ? value : 0;
    }

    private readonly record struct TravelArea(
        int MinimumX,
        int MinimumY,
        int MaximumX,
        int MaximumY);
}
