using System.Text.Json;
using AA.Annotate.Core.Geometry;
using AA.Annotate.Platform;
using AA.Annotate.Platform.Linux;

if (args.Length is < 3 or > 5 ||
    !int.TryParse(args[1], out var width) ||
    !int.TryParse(args[2], out var height) ||
    width <= 0 ||
    height <= 0)
{
    Console.Error.WriteLine(
        "Usage: AA.Annotate.Platform.Linux.Smoke <output.png> <width> <height> [x] [y]");
    return 64;
}

var x = args.Length >= 4 && int.TryParse(args[3], out var parsedX) ? parsedX : 0;
var y = args.Length >= 5 && int.TryParse(args[4], out var parsedY) ? parsedY : 0;
var outputPath = Path.GetFullPath(args[0]);
var display = new DisplayDescriptor(
    "smoke-display",
    "VM smoke display",
    new RectInt(x, y, width, height),
    IsPrimary: true);
var startedAt = DateTimeOffset.UtcNow;
var result = await new LinuxScreenCaptureService().CaptureScreenAsync(
    new ScreenCaptureRequest(
        outputPath,
        display,
        IncludeCursor: false,
        CancellationToken.None));
var finishedAt = DateTimeOffset.UtcNow;

Console.WriteLine(JsonSerializer.Serialize(
    new
    {
        startedAtUtc = startedAt,
        finishedAtUtc = finishedAt,
        session = new LinuxDesktopSessionDetector().Detect(),
        outcome = result.Outcome.ToString(),
        result.ErrorMessage,
        screenshotPath = result.CapturedScreen?.ScreenshotPath,
        pixelSize = result.CapturedScreen?.PixelSize,
        fileExists = File.Exists(outputPath)
    },
    new JsonSerializerOptions { WriteIndented = true }));

return result.Outcome switch
{
    ScreenCaptureOutcome.Completed => 0,
    ScreenCaptureOutcome.Cancelled => 2,
    _ => 1
};
