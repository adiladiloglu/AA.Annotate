using AA.Annotate.Core.Geometry;

namespace AA.Annotate.Platform;

public sealed record CapturedScreen(
    DisplayDescriptor Display,
    string ScreenshotPath,
    SizeInt PixelSize);
