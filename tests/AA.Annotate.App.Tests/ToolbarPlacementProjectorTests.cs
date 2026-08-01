using AA.Annotate.App.ViewModels;
using Avalonia;

namespace AA.Annotate.App.Tests;

public sealed class ToolbarPlacementProjectorTests
{
    [Theory]
    [InlineData(1.0)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2.0)]
    public void ProjectAndRestoreRoundTripAtCommonDisplayScales(double scaling)
    {
        var display = Display(
            "display-a",
            bounds: new PixelRect(0, 0, 2560, 1440),
            workingArea: new PixelRect(0, 0, 2560, 1400),
            scaling: scaling,
            isPrimary: true);
        var original = new PixelPoint(731, 419);

        var placement = ToolbarPlacementProjector.Project(
            original,
            new Size(420, 56),
            [display]);
        var restored = ToolbarPlacementProjector.Restore(
            placement,
            new Size(420, 56),
            [display]);

        Assert.InRange(Math.Abs(restored.X - original.X), 0, 1);
        Assert.InRange(Math.Abs(restored.Y - original.Y), 0, 1);
    }

    [Fact]
    public void NegativeDisplayOriginIsPreserved()
    {
        var display = Display(
            "left",
            new PixelRect(-1920, -160, 1920, 1080),
            new PixelRect(-1920, -120, 1920, 1040),
            isPrimary: false);
        var original = new PixelPoint(-1600, 96);

        var placement = ToolbarPlacementProjector.Project(
            original,
            new Size(400, 60),
            [display]);
        var restored = ToolbarPlacementProjector.Restore(
            placement,
            new Size(400, 60),
            [display]);

        Assert.InRange(Math.Abs(restored.X - original.X), 0, 1);
        Assert.InRange(Math.Abs(restored.Y - original.Y), 0, 1);
    }

    [Fact]
    public void RestoreUsesWorkingAreaAndLeavesPhysicalMargin()
    {
        var display = Display(
            "primary",
            new PixelRect(0, 0, 1920, 1080),
            new PixelRect(80, 30, 1840, 1010),
            scaling: 1.5,
            isPrimary: true);
        var placement = new ToolbarPlacement(
            display.Name,
            display.Bounds,
            normalizedX: 1,
            normalizedY: 1);

        var restored = ToolbarPlacementProjector.Restore(
            placement,
            new Size(400, 60),
            [display]);

        Assert.Equal(1308, restored.X);
        Assert.Equal(938, restored.Y);
    }

    [Fact]
    public void RestoreMatchesDisplayNameBeforePriorGeometry()
    {
        var primary = Display(
            "primary",
            new PixelRect(0, 0, 1920, 1080),
            new PixelRect(0, 0, 1920, 1040),
            isPrimary: true);
        var moved = Display(
            "saved",
            new PixelRect(1920, 0, 2560, 1440),
            new PixelRect(1920, 0, 2560, 1400),
            isPrimary: false);
        var placement = new ToolbarPlacement(
            "saved",
            primary.Bounds,
            normalizedX: 0,
            normalizedY: 0);

        var restored = ToolbarPlacementProjector.Restore(
            placement,
            new Size(400, 60),
            [primary, moved]);

        Assert.Equal(new PixelPoint(1928, 8), restored);
    }

    [Fact]
    public void RestoreUsesPriorBoundsWhenDisplayWasRenamed()
    {
        var primary = Display(
            "primary",
            new PixelRect(0, 0, 1920, 1080),
            new PixelRect(0, 0, 1920, 1040),
            isPrimary: true);
        var renamed = Display(
            "new-name",
            new PixelRect(-2560, 0, 2560, 1440),
            new PixelRect(-2560, 0, 2560, 1400),
            isPrimary: false);
        var placement = new ToolbarPlacement(
            "old-name",
            renamed.Bounds,
            normalizedX: 0,
            normalizedY: 0);

        var restored = ToolbarPlacementProjector.Restore(
            placement,
            new Size(400, 60),
            [primary, renamed]);

        Assert.Equal(new PixelPoint(-2552, 8), restored);
    }

    [Fact]
    public void RestoreFallsBackToPrimaryAfterDisplayRemoval()
    {
        var primary = Display(
            "primary",
            new PixelRect(0, 0, 1920, 1080),
            new PixelRect(0, 0, 1920, 1040),
            isPrimary: true);
        var placement = new ToolbarPlacement(
            "removed",
            new PixelRect(3000, 0, 1920, 1080),
            normalizedX: 0.5,
            normalizedY: 0.5);

        var restored = ToolbarPlacementProjector.Restore(
            placement,
            new Size(400, 60),
            [primary]);

        Assert.Equal(new PixelPoint(760, 490), restored);
    }

    [Fact]
    public void InvalidNormalizedValuesRestoreToReachableOrigin()
    {
        var display = Display(
            "primary",
            new PixelRect(0, 0, 1920, 1080),
            new PixelRect(0, 0, 1920, 1040),
            isPrimary: true);
        var placement = new ToolbarPlacement(
            display.Name,
            display.Bounds,
            normalizedX: double.NaN,
            normalizedY: double.PositiveInfinity);

        var restored = ToolbarPlacementProjector.Restore(
            placement,
            new Size(400, 60),
            [display]);

        Assert.Equal(new PixelPoint(8, 8), restored);
    }

    [Fact]
    public void OutOfRangeNormalizedValuesAreClamped()
    {
        var display = Display(
            "primary",
            new PixelRect(0, 0, 1920, 1080),
            new PixelRect(0, 0, 1920, 1040),
            isPrimary: true);
        var placement = new ToolbarPlacement(
            display.Name,
            display.Bounds,
            normalizedX: -5,
            normalizedY: 12);

        var restored = ToolbarPlacementProjector.Restore(
            placement,
            new Size(400, 60),
            [display]);

        Assert.Equal(new PixelPoint(8, 972), restored);
    }

    [Fact]
    public void OversizedToolbarKeepsTopLeftReachable()
    {
        var display = Display(
            "small",
            new PixelRect(0, 0, 800, 600),
            new PixelRect(0, 0, 800, 560),
            isPrimary: true);

        var restored = ToolbarPlacementProjector.Restore(
            new ToolbarPlacement("small", display.Bounds, 1, 1),
            new Size(1200, 900),
            [display]);

        Assert.Equal(new PixelPoint(8, 8), restored);
    }

    [Fact]
    public void FirstLaunchUsesTopCenterWithScaledInsetOnPrimaryDisplay()
    {
        var secondary = Display(
            "secondary",
            new PixelRect(-1920, 0, 1920, 1080),
            new PixelRect(-1920, 0, 1920, 1040),
            isPrimary: false);
        var primary = Display(
            "primary",
            new PixelRect(0, 0, 2560, 1440),
            new PixelRect(0, 40, 2560, 1400),
            scaling: 1.5,
            isPrimary: true);

        var restored = ToolbarPlacementProjector.Restore(
            placement: null,
            new Size(400, 60),
            [secondary, primary]);

        Assert.Equal(new PixelPoint(980, 76), restored);
    }

    [Fact]
    public void FirstLaunchCentersOnPrimaryDisplayWithNegativeOrigin()
    {
        var primary = Display(
            "primary",
            new PixelRect(-2560, -120, 2560, 1440),
            new PixelRect(-2560, -80, 2560, 1400),
            scaling: 1.25,
            isPrimary: true);

        var restored = ToolbarPlacementProjector.Restore(
            placement: null,
            new Size(400, 60),
            [primary]);

        Assert.Equal(new PixelPoint(-1530, -50), restored);
    }

    [Fact]
    public void ProjectChoosesDisplayWithLargestToolbarOverlap()
    {
        var left = Display(
            "left",
            new PixelRect(0, 0, 1000, 800),
            new PixelRect(0, 0, 1000, 760),
            isPrimary: true);
        var right = Display(
            "right",
            new PixelRect(1000, 0, 1000, 800),
            new PixelRect(1000, 0, 1000, 760),
            isPrimary: false);

        var placement = ToolbarPlacementProjector.Project(
            new PixelPoint(850, 100),
            new Size(400, 60),
            [left, right]);

        Assert.Equal("right", placement.DisplayName);
        Assert.Equal(right.Bounds, placement.DisplayBounds);
    }

    [Fact]
    public void ProjectUsesActualWindowScalingAcrossMixedDpiBoundary()
    {
        var left = Display(
            "left-100",
            new PixelRect(0, 0, 1000, 800),
            new PixelRect(0, 0, 1000, 760),
            scaling: 1,
            isPrimary: true);
        var right = Display(
            "right-200",
            new PixelRect(1000, 0, 1000, 800),
            new PixelRect(1000, 0, 1000, 760),
            scaling: 2);

        var placement = ToolbarPlacementProjector.Project(
            new PixelPoint(650, 100),
            new Size(400, 60),
            [left, right],
            toolbarScaling: 1);

        Assert.Equal("left-100", placement.DisplayName);
    }

    [Fact]
    public void EmptyDisplayListIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            ToolbarPlacementProjector.Project(
                new PixelPoint(),
                new Size(400, 60),
                []));
        Assert.Throws<ArgumentException>(() =>
            ToolbarPlacementProjector.Restore(
                placement: null,
                new Size(400, 60),
                []));
    }

    private static ToolbarDisplay Display(
        string name,
        PixelRect bounds,
        PixelRect workingArea,
        double scaling = 1,
        bool isPrimary = false)
    {
        return new ToolbarDisplay(name, bounds, workingArea, scaling, isPrimary);
    }
}
