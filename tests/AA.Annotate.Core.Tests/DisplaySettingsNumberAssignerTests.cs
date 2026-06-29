using AA.Annotate.Core.Geometry;

namespace AA.Annotate.Core.Tests;

public sealed class DisplaySettingsNumberAssignerTests
{
    [Fact]
    public void AssignNumbersMatchesWindowsSettingsForPrimaryCenterLayout()
    {
        var assignments = DisplaySettingsNumberAssigner.Assign(
            [
                new DisplaySettingsNumberSource(0, new RectInt(2560, 509, 1920, 1080), IsPrimary: false),
                new DisplaySettingsNumberSource(1, new RectInt(0, 0, 2560, 1600), IsPrimary: true),
                new DisplaySettingsNumberSource(2, new RectInt(-1920, 511, 1920, 1080), IsPrimary: false)
            ]);

        Assert.Equal(3, assignments[0]);
        Assert.Equal(1, assignments[1]);
        Assert.Equal(2, assignments[2]);
    }

    [Fact]
    public void AssignNumbersUsesLeftToRightOrderWhenNoPrimaryIsKnown()
    {
        var assignments = DisplaySettingsNumberAssigner.Assign(
            [
                new DisplaySettingsNumberSource(0, new RectInt(1920, 0, 1920, 1080), IsPrimary: false),
                new DisplaySettingsNumberSource(1, new RectInt(0, 0, 1920, 1080), IsPrimary: false)
            ]);

        Assert.Equal(2, assignments[0]);
        Assert.Equal(1, assignments[1]);
    }
}
