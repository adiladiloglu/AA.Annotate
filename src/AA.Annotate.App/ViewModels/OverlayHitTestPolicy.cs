using AA.Annotate.Core.Geometry;

namespace AA.Annotate.App.ViewModels;

public static class OverlayHitTestPolicy
{
    public static bool ShouldHandlePoint(
        bool handleFullSurface,
        IEnumerable<RectInt> interactiveRects,
        PointInt point)
    {
        return handleFullSurface || interactiveRects.Any(rect => Contains(rect, point));
    }

    private static bool Contains(RectInt rect, PointInt point)
    {
        return point.X >= rect.X &&
            point.Y >= rect.Y &&
            point.X < rect.X + rect.Width &&
            point.Y < rect.Y + rect.Height;
    }
}
