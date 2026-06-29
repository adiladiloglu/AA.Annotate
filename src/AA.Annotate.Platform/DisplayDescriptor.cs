using AA.Annotate.Core.Geometry;

namespace AA.Annotate.Platform;

public sealed record DisplayDescriptor(
    string? Id,
    string? Name,
    RectInt Bounds,
    bool IsPrimary);
