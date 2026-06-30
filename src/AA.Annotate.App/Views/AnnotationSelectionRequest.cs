using AA.Annotate.App.ViewModels;
using AA.Annotate.Core.Geometry;

namespace AA.Annotate.App.Views;

public sealed record AnnotationSelectionRequest(
    AnnotationViewModel Annotation,
    PointInt Point);
