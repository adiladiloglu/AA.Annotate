using AA.Annotate.Core.Geometry;

namespace AA.Annotate.Core.Models;

public sealed record CaptureDisplay(
    string? DisplayId,
    string? DisplayName,
    RectInt Bounds);
