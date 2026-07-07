using AA.Annotate.Core.Geometry;

namespace AA.Annotate.Core.Models;

public sealed record PrivacyMask(
    string MaskId,
    RectInt BoxRect);
