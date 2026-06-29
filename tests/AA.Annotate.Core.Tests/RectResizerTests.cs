using AA.Annotate.Core.Geometry;

namespace AA.Annotate.Core.Tests;

public sealed class RectResizerTests
{
    [Fact]
    public void ResizeLeftKeepsRightEdgeFixed()
    {
        var resized = RectResizer.Resize(
            new RectInt(100, 80, 300, 200),
            RectResizeHandle.Left,
            new PointInt(-40, 0),
            new SizeInt(800, 600),
            minSize: 48);

        Assert.Equal(new RectInt(60, 80, 340, 200), resized);
    }

    [Fact]
    public void ResizeTopKeepsBottomEdgeFixed()
    {
        var resized = RectResizer.Resize(
            new RectInt(100, 80, 300, 200),
            RectResizeHandle.Top,
            new PointInt(0, 30),
            new SizeInt(800, 600),
            minSize: 48);

        Assert.Equal(new RectInt(100, 110, 300, 170), resized);
    }

    [Fact]
    public void ResizeTopRightCombinesTopAndRightEdges()
    {
        var resized = RectResizer.Resize(
            new RectInt(100, 80, 300, 200),
            RectResizeHandle.TopRight,
            new PointInt(25, -20),
            new SizeInt(800, 600),
            minSize: 48);

        Assert.Equal(new RectInt(100, 60, 325, 220), resized);
    }

    [Fact]
    public void ResizeBottomLeftCombinesBottomAndLeftEdges()
    {
        var resized = RectResizer.Resize(
            new RectInt(100, 80, 300, 200),
            RectResizeHandle.BottomLeft,
            new PointInt(25, 40),
            new SizeInt(800, 600),
            minSize: 48);

        Assert.Equal(new RectInt(125, 80, 275, 240), resized);
    }

    [Fact]
    public void ResizeClampsToMinimumSizeAndBounds()
    {
        var resized = RectResizer.Resize(
            new RectInt(100, 80, 300, 200),
            RectResizeHandle.TopLeft,
            new PointInt(500, 500),
            new SizeInt(800, 600),
            minSize: 48);

        Assert.Equal(new RectInt(352, 232, 48, 48), resized);
    }
}
