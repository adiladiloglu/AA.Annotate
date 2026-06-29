namespace AA.Annotate.Core.Geometry;

public static class DisplaySettingsNumberAssigner
{
    public static IReadOnlyDictionary<int, int> Assign(IReadOnlyList<DisplaySettingsNumberSource> displays)
    {
        var ordered = displays
            .OrderByDescending(display => display.IsPrimary)
            .ThenBy(display => display.Bounds.X)
            .ThenBy(display => display.Bounds.Y)
            .ThenBy(display => display.OriginalIndex)
            .Select((display, index) => new
            {
                display.OriginalIndex,
                Number = index + 1
            });

        return ordered.ToDictionary(item => item.OriginalIndex, item => item.Number);
    }
}
