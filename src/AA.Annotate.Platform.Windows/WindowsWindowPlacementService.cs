using AA.Annotate.Core.Geometry;
using AA.Annotate.Platform;

namespace AA.Annotate.Platform.Windows;

public sealed class WindowsWindowPlacementService : IWindowPlacementService
{
    public PointInt ConstrainPointToDisplay(PointInt point, DisplayDescriptor display)
    {
        var x = Math.Clamp(point.X, display.Bounds.X, display.Bounds.Right);
        var y = Math.Clamp(point.Y, display.Bounds.Y, display.Bounds.Bottom);
        return new PointInt(x, y);
    }
}
