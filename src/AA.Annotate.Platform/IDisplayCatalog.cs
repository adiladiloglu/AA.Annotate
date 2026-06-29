using AA.Annotate.Core.Geometry;

namespace AA.Annotate.Platform;

public interface IDisplayCatalog
{
    IReadOnlyList<DisplayDescriptor> GetDisplays();

    DisplayDescriptor GetDisplayContainingPoint(PointInt point);
}
