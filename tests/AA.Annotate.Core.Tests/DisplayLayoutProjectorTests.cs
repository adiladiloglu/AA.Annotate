using AA.Annotate.Core.Geometry;

namespace AA.Annotate.Core.Tests;

public sealed class DisplayLayoutProjectorTests
{
    [Fact]
    public void ProjectPreservesHorizontalScreenPlacement()
    {
        var projected = DisplayLayoutProjector.Project(
            [
                new RectInt(0, 0, 2560, 1600),
                new RectInt(-1920, 260, 1920, 1080),
                new RectInt(2560, 260, 1920, 1080)
            ],
            new SizeInt(340, 130),
            padding: 10);

        Assert.Equal(3, projected.Count);
        Assert.True(projected[1].Right <= projected[0].X);
        Assert.True(projected[0].Right <= projected[2].X);
        Assert.True(projected[0].Height > projected[1].Height);
        Assert.True(projected[0].Height > projected[2].Height);
    }

    [Fact]
    public void ProjectCentersLayoutInsideViewport()
    {
        var projected = DisplayLayoutProjector.Project(
            [
                new RectInt(0, 0, 1920, 1080),
                new RectInt(1920, 0, 1920, 1080)
            ],
            new SizeInt(300, 120),
            padding: 12);

        Assert.True(projected.All(rect => rect.X >= 12));
        Assert.True(projected.All(rect => rect.Y >= 12));
        Assert.True(projected.All(rect => rect.Right <= 288));
        Assert.True(projected.All(rect => rect.Bottom <= 108));
    }
}
