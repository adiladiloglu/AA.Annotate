using AA.Annotate.Core.Geometry;

namespace AA.Annotate.Platform;

public interface IWindowPlacementService
{
    PointInt ConstrainPointToDisplay(PointInt point, DisplayDescriptor display);
}
