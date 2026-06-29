using System.Windows.Forms;
using AA.Annotate.Core.Geometry;
using AA.Annotate.Platform;

namespace AA.Annotate.Platform.Windows;

public sealed class WindowsDisplayCatalog : IDisplayCatalog
{
    public IReadOnlyList<DisplayDescriptor> GetDisplays()
    {
        return Screen.AllScreens
            .Select(screen => new DisplayDescriptor(
                screen.DeviceName,
                screen.DeviceName,
                new RectInt(screen.Bounds.X, screen.Bounds.Y, screen.Bounds.Width, screen.Bounds.Height),
                screen.Primary))
            .ToList();
    }

    public DisplayDescriptor GetDisplayContainingPoint(PointInt point)
    {
        var displays = GetDisplays();
        return displays.FirstOrDefault(display =>
            point.X >= display.Bounds.X &&
            point.X < display.Bounds.Right &&
            point.Y >= display.Bounds.Y &&
            point.Y < display.Bounds.Bottom)
            ?? displays.First(display => display.IsPrimary);
    }
}
