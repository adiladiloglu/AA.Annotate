using AA.Annotate.Core.Geometry;

namespace AA.Annotate.App.ViewModels;

public static class AnnotationRectPolicy
{
    public const int MinimumSize = 12;

    public static RectInt CreateFromDrag(PointInt start, PointInt current, SizeInt bounds)
    {
        var deltaX = current.X - start.X;
        var deltaY = current.Y - start.Y;
        var width = Math.Abs(deltaX);
        var height = Math.Abs(deltaY);
        var x = deltaX < 0 ? start.X - width : start.X;
        var y = deltaY < 0 ? start.Y - height : start.Y;

        return ClampRawToBounds(new RectInt(x, y, width, height), bounds);
    }

    public static bool IsMinimumSizeReached(RectInt rect, int minimumSize = MinimumSize)
    {
        return rect.Width >= minimumSize && rect.Height >= minimumSize;
    }

    public static RectInt ClampToBounds(RectInt rect, SizeInt bounds, int minimumSize = MinimumSize)
    {
        var boundWidth = Math.Max(1, bounds.Width);
        var boundHeight = Math.Max(1, bounds.Height);
        var minWidth = Math.Min(Math.Max(1, minimumSize), boundWidth);
        var minHeight = Math.Min(Math.Max(1, minimumSize), boundHeight);
        var width = Math.Clamp(rect.Width, minWidth, boundWidth);
        var height = Math.Clamp(rect.Height, minHeight, boundHeight);
        var x = Math.Clamp(rect.X, 0, Math.Max(0, boundWidth - width));
        var y = Math.Clamp(rect.Y, 0, Math.Max(0, boundHeight - height));

        return new RectInt(x, y, width, height);
    }

    private static RectInt ClampRawToBounds(RectInt rect, SizeInt bounds)
    {
        var boundWidth = Math.Max(1, bounds.Width);
        var boundHeight = Math.Max(1, bounds.Height);
        var x = Math.Clamp(rect.X, 0, Math.Max(0, boundWidth - 1));
        var y = Math.Clamp(rect.Y, 0, Math.Max(0, boundHeight - 1));
        var width = Math.Clamp(rect.Width, 0, boundWidth - x);
        var height = Math.Clamp(rect.Height, 0, boundHeight - y);

        return new RectInt(x, y, width, height);
    }
}
