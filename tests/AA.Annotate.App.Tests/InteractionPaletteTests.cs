using AA.Annotate.App.Styling;
using Avalonia.Media;

namespace AA.Annotate.App.Tests;

public sealed class InteractionPaletteTests
{
    public static TheoryData<string, string> ForegroundBackgroundPairs => new()
    {
        { InteractionPalette.PrimaryText, InteractionPalette.ControlHover },
        { InteractionPalette.PrimaryText, InteractionPalette.ControlPressed },
        { InteractionPalette.PrimaryText, InteractionPalette.PanelItemHover },
        { InteractionPalette.PrimaryText, InteractionPalette.PanelItemPressed },
        { InteractionPalette.PrimaryText, InteractionPalette.PanelItemSelectedHover },
        { InteractionPalette.PrimaryText, InteractionPalette.PanelItemSelectedPressed },
        { InteractionPalette.ActiveControlForeground, InteractionPalette.ActiveControl },
        { InteractionPalette.ActiveControlForeground, InteractionPalette.ActiveControlHover },
        { InteractionPalette.ActiveControlForeground, InteractionPalette.ActiveControlPressed },
        { InteractionPalette.PrimaryText, InteractionPalette.Danger },
        { InteractionPalette.PrimaryText, InteractionPalette.DangerHover },
        { InteractionPalette.PrimaryText, InteractionPalette.DangerPressed },
        { InteractionPalette.Confirm, InteractionPalette.ConfirmSurface },
        { InteractionPalette.ConfirmHover, InteractionPalette.ConfirmHoverSurface },
        { InteractionPalette.ConfirmPressed, InteractionPalette.ConfirmPressedSurface }
    };

    [Theory]
    [MemberData(nameof(ForegroundBackgroundPairs))]
    public void InteractiveForegroundsMeetNormalTextContrast(string foreground, string background)
    {
        Assert.True(
            ContrastRatio(Color.Parse(foreground), Color.Parse(background)) >= 4.5,
            $"{foreground} on {background} does not meet the 4.5:1 contrast target.");
    }

    [Fact]
    public void EveryInteractionFamilyHasDistinctRestHoverAndPressedColors()
    {
        AssertDistinct(
            InteractionPalette.PanelItem,
            InteractionPalette.PanelItemHover,
            InteractionPalette.PanelItemPressed);
        AssertDistinct(
            InteractionPalette.PanelItemSelected,
            InteractionPalette.PanelItemSelectedHover,
            InteractionPalette.PanelItemSelectedPressed);
        AssertDistinct(
            InteractionPalette.ActiveControl,
            InteractionPalette.ActiveControlHover,
            InteractionPalette.ActiveControlPressed);
        AssertDistinct(
            InteractionPalette.Danger,
            InteractionPalette.DangerHover,
            InteractionPalette.DangerPressed);
        AssertDistinct(
            InteractionPalette.ConfirmSurface,
            InteractionPalette.ConfirmHoverSurface,
            InteractionPalette.ConfirmPressedSurface);
    }

    private static void AssertDistinct(string rest, string hover, string pressed)
    {
        Assert.NotEqual(rest, hover);
        Assert.NotEqual(rest, pressed);
        Assert.NotEqual(hover, pressed);
    }

    private static double ContrastRatio(Color foreground, Color background)
    {
        var foregroundLuminance = RelativeLuminance(foreground);
        var backgroundLuminance = RelativeLuminance(background);
        return (Math.Max(foregroundLuminance, backgroundLuminance) + 0.05)
            / (Math.Min(foregroundLuminance, backgroundLuminance) + 0.05);
    }

    private static double RelativeLuminance(Color color)
    {
        return (0.2126 * Linearize(color.R / 255d))
            + (0.7152 * Linearize(color.G / 255d))
            + (0.0722 * Linearize(color.B / 255d));
    }

    private static double Linearize(double channel)
    {
        return channel <= 0.04045
            ? channel / 12.92
            : Math.Pow((channel + 0.055) / 1.055, 2.4);
    }
}
