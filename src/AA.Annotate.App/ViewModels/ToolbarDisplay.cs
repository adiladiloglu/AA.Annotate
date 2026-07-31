using Avalonia;

namespace AA.Annotate.App.ViewModels;

public sealed record ToolbarDisplay(
    string Name,
    PixelRect Bounds,
    PixelRect WorkingArea,
    double Scaling,
    bool IsPrimary);
