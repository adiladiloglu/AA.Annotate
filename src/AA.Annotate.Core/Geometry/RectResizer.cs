namespace AA.Annotate.Core.Geometry;

public static class RectResizer
{
    public static RectInt Resize(RectInt origin, RectResizeHandle handle, PointInt delta, SizeInt bounds, int minSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minSize, 1);

        var maxWidth = Math.Max(1, bounds.Width);
        var maxHeight = Math.Max(1, bounds.Height);
        var minWidth = Math.Min(minSize, maxWidth);
        var minHeight = Math.Min(minSize, maxHeight);

        var left = Clamp(origin.X, 0, maxWidth - minWidth);
        var top = Clamp(origin.Y, 0, maxHeight - minHeight);
        var right = Clamp(origin.Right, left + minWidth, maxWidth);
        var bottom = Clamp(origin.Bottom, top + minHeight, maxHeight);

        if (UsesLeft(handle))
        {
            left = Clamp(origin.X + delta.X, 0, right - minWidth);
        }

        if (UsesTop(handle))
        {
            top = Clamp(origin.Y + delta.Y, 0, bottom - minHeight);
        }

        if (UsesRight(handle))
        {
            right = Clamp(origin.Right + delta.X, left + minWidth, maxWidth);
        }

        if (UsesBottom(handle))
        {
            bottom = Clamp(origin.Bottom + delta.Y, top + minHeight, maxHeight);
        }

        return new RectInt(left, top, right - left, bottom - top);
    }

    private static bool UsesLeft(RectResizeHandle handle)
    {
        return handle is RectResizeHandle.Left or RectResizeHandle.TopLeft or RectResizeHandle.BottomLeft;
    }

    private static bool UsesTop(RectResizeHandle handle)
    {
        return handle is RectResizeHandle.Top or RectResizeHandle.TopLeft or RectResizeHandle.TopRight;
    }

    private static bool UsesRight(RectResizeHandle handle)
    {
        return handle is RectResizeHandle.Right or RectResizeHandle.TopRight or RectResizeHandle.BottomRight;
    }

    private static bool UsesBottom(RectResizeHandle handle)
    {
        return handle is RectResizeHandle.Bottom or RectResizeHandle.BottomLeft or RectResizeHandle.BottomRight;
    }

    private static int Clamp(int value, int min, int max)
    {
        return Math.Clamp(value, min, Math.Max(min, max));
    }
}
