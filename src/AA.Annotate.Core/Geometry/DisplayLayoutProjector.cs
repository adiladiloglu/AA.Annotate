namespace AA.Annotate.Core.Geometry;

public static class DisplayLayoutProjector
{
    public static IReadOnlyList<RectInt> Project(IReadOnlyList<RectInt> displays, SizeInt viewport, int padding)
    {
        if (displays.Count == 0)
        {
            return [];
        }

        var viewportWidth = Math.Max(1, viewport.Width);
        var viewportHeight = Math.Max(1, viewport.Height);
        var inset = Math.Clamp(padding, 0, Math.Min(viewportWidth, viewportHeight) / 2);
        var contentWidth = Math.Max(1, viewportWidth - inset * 2);
        var contentHeight = Math.Max(1, viewportHeight - inset * 2);

        var left = displays.Min(display => display.X);
        var top = displays.Min(display => display.Y);
        var right = displays.Max(display => display.Right);
        var bottom = displays.Max(display => display.Bottom);
        var virtualWidth = Math.Max(1, right - left);
        var virtualHeight = Math.Max(1, bottom - top);
        var scale = Math.Min(contentWidth / (double)virtualWidth, contentHeight / (double)virtualHeight);
        var offsetX = inset + (contentWidth - virtualWidth * scale) / 2;
        var offsetY = inset + (contentHeight - virtualHeight * scale) / 2;

        return displays
            .Select(display => new RectInt(
                (int)Math.Round(offsetX + (display.X - left) * scale),
                (int)Math.Round(offsetY + (display.Y - top) * scale),
                Math.Max(1, (int)Math.Round(display.Width * scale)),
                Math.Max(1, (int)Math.Round(display.Height * scale))))
            .ToList();
    }
}
