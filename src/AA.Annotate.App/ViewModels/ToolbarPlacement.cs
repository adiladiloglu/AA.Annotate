using System.Text.Json.Serialization;
using Avalonia;

namespace AA.Annotate.App.ViewModels;

public sealed record ToolbarPlacement
{
    [JsonConstructor]
    public ToolbarPlacement(
        string? displayName,
        int displayX,
        int displayY,
        int displayWidth,
        int displayHeight,
        double normalizedX,
        double normalizedY)
    {
        DisplayName = displayName;
        DisplayX = displayX;
        DisplayY = displayY;
        DisplayWidth = displayWidth;
        DisplayHeight = displayHeight;
        NormalizedX = normalizedX;
        NormalizedY = normalizedY;
    }

    public ToolbarPlacement(
        string? displayName,
        PixelRect displayBounds,
        double normalizedX,
        double normalizedY)
        : this(
            displayName,
            displayBounds.X,
            displayBounds.Y,
            displayBounds.Width,
            displayBounds.Height,
            normalizedX,
            normalizedY)
    {
    }

    public string? DisplayName { get; init; }

    public int DisplayX { get; init; }

    public int DisplayY { get; init; }

    public int DisplayWidth { get; init; }

    public int DisplayHeight { get; init; }

    public double NormalizedX { get; init; }

    public double NormalizedY { get; init; }

    [JsonIgnore]
    public PixelRect DisplayBounds => new(
        DisplayX,
        DisplayY,
        Math.Max(0, DisplayWidth),
        Math.Max(0, DisplayHeight));
}
