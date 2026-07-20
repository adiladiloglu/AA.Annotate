using AA.Annotate.Core.Geometry;
using AA.Annotate.Platform;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace AA.Annotate.App.Services;

internal sealed class AvaloniaDisplayCatalog(Func<Screens> screensProvider) : IDisplayCatalog
{
    private readonly Func<Screens> _screensProvider =
        screensProvider ?? throw new ArgumentNullException(nameof(screensProvider));

    public IReadOnlyList<DisplayDescriptor> GetDisplays()
    {
        return _screensProvider()
            .All
            .Select(CreateDescriptor)
            .ToList();
    }

    public DisplayDescriptor GetDisplayContainingPoint(PointInt point)
    {
        var screens = _screensProvider();
        var screen = screens.ScreenFromPoint(new PixelPoint(point.X, point.Y))
            ?? screens.Primary
            ?? screens.All.FirstOrDefault()
            ?? throw new InvalidOperationException("No displays are available.");

        return CreateDescriptor(screen);
    }

    private static DisplayDescriptor CreateDescriptor(Screen screen)
    {
        var bounds = screen.Bounds;
        var nativeId = CreateNativeDisplayId(screen);
        return new DisplayDescriptor(
            nativeId ?? CreateFallbackDisplayId(screen),
            screen.DisplayName,
            new RectInt(bounds.X, bounds.Y, bounds.Width, bounds.Height),
            screen.IsPrimary,
            nativeId,
            screen.Scaling);
    }

    private static string? CreateNativeDisplayId(Screen screen)
    {
        IPlatformHandle? handle = screen.TryGetPlatformHandle();
        if (handle is not null && handle.Handle != 0)
        {
            return $"{handle.HandleDescriptor}:{handle.Handle.ToInt64():X}";
        }

        return null;
    }

    private static string CreateFallbackDisplayId(Screen screen)
    {
        var bounds = screen.Bounds;
        return $"avalonia:{bounds.X},{bounds.Y},{bounds.Width},{bounds.Height}:{screen.DisplayName}";
    }
}
