using AA.Annotate.Core.Geometry;
using AA.Annotate.Core.Services;

namespace AA.Annotate.Core.Tests;

public sealed class AnnotationCropPolicyTests
{
    [Fact]
    public void ClassifyReturnsIncludedWhenAnnotationIsFullyInsideCrop()
    {
        var result = AnnotationCropPolicy.Classify(
            new RectInt(150, 120, 50, 40),
            new RectInt(100, 100, 300, 200));

        Assert.Equal(AnnotationCropExportState.Included, result.State);
        Assert.Equal(new RectInt(50, 20, 50, 40), result.ExportBoxRect);
    }

    [Fact]
    public void ClassifyReturnsClippedWhenAnnotationPartlyOverlapsCrop()
    {
        var result = AnnotationCropPolicy.Classify(
            new RectInt(80, 150, 50, 80),
            new RectInt(100, 100, 300, 200));

        Assert.Equal(AnnotationCropExportState.Clipped, result.State);
        Assert.Equal(new RectInt(0, 50, 30, 80), result.ExportBoxRect);
    }

    [Fact]
    public void ClassifyReturnsExcludedWhenAnnotationIsOutsideCrop()
    {
        var result = AnnotationCropPolicy.Classify(
            new RectInt(10, 10, 20, 20),
            new RectInt(100, 100, 300, 200));

        Assert.Equal(AnnotationCropExportState.Excluded, result.State);
        Assert.Null(result.ExportBoxRect);
    }
}
