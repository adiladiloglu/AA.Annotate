using AA.Annotate.Core.Geometry;

namespace AA.Annotate.Core.Models;

public sealed record Annotation(
    string AnnotationId,
    int Number,
    RectInt BoxRect,
    string Comment,
    string? ImagePath = null);
