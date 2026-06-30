using AA.Annotate.Core.Geometry;

namespace AA.Annotate.App.ViewModels;

public static class AnnotationSelectionPolicy
{
    public static AnnotationViewModel SelectAtPoint(
        IEnumerable<AnnotationViewModel> annotations,
        AnnotationViewModel? currentSelection,
        AnnotationViewModel requested,
        PointInt point)
    {
        var hits = annotations
            .OrderByDescending(annotation => annotation.Number)
            .Where(annotation => Contains(annotation.BoxRect, point))
            .ToList();

        if (hits.Count == 0)
        {
            return requested;
        }

        var selectedIndex = currentSelection is null ? -1 : hits.IndexOf(currentSelection);
        if (selectedIndex >= 0)
        {
            return hits[(selectedIndex + 1) % hits.Count];
        }

        return hits[0];
    }

    private static bool Contains(RectInt rect, PointInt point)
    {
        return point.X >= rect.X &&
            point.X < rect.Right &&
            point.Y >= rect.Y &&
            point.Y < rect.Bottom;
    }
}
