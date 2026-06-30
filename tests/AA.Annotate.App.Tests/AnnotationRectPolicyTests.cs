using AA.Annotate.App.ViewModels;
using AA.Annotate.Core.Geometry;

namespace AA.Annotate.App.Tests;

public sealed class AnnotationRectPolicyTests
{
    [Fact]
    public void CreateFromDragKeepsActualSizeWhileDrawingDownRight()
    {
        var rect = AnnotationRectPolicy.CreateFromDrag(
            new PointInt(100, 100),
            new PointInt(104, 105),
            new SizeInt(200, 200));

        Assert.Equal(new RectInt(100, 100, 4, 5), rect);
    }

    [Fact]
    public void CreateFromDragKeepsActualSizeWhileDrawingUpLeft()
    {
        var rect = AnnotationRectPolicy.CreateFromDrag(
            new PointInt(100, 100),
            new PointInt(96, 95),
            new SizeInt(200, 200));

        Assert.Equal(new RectInt(96, 95, 4, 5), rect);
    }

    [Fact]
    public void CreateFromDragKeepsActualSizeInsideBottomRightBounds()
    {
        var rect = AnnotationRectPolicy.CreateFromDrag(
            new PointInt(2555, 1595),
            new PointInt(2556, 1596),
            new SizeInt(2560, 1600));

        Assert.Equal(new RectInt(2555, 1595, 1, 1), rect);
    }

    [Theory]
    [InlineData(11, 12, false)]
    [InlineData(12, 11, false)]
    [InlineData(12, 12, true)]
    public void IsMinimumSizeReachedRequiresBothDimensionsToMeetMinimum(int width, int height, bool expected)
    {
        Assert.Equal(expected, AnnotationRectPolicy.IsMinimumSizeReached(new RectInt(0, 0, width, height)));
    }
}
