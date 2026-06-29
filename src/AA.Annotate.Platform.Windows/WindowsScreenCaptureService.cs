using System.Drawing;
using System.Drawing.Imaging;
using AA.Annotate.Core.Geometry;
using AA.Annotate.Platform;

namespace AA.Annotate.Platform.Windows;

public sealed class WindowsScreenCaptureService : IScreenCaptureService
{
    public Task<CapturedScreen> CaptureScreenAsync(
        DisplayDescriptor display,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");

        using var bitmap = new Bitmap(display.Bounds.Width, display.Bounds.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(
            display.Bounds.X,
            display.Bounds.Y,
            0,
            0,
            new Size(display.Bounds.Width, display.Bounds.Height),
            CopyPixelOperation.SourceCopy);
        bitmap.Save(outputPath, ImageFormat.Png);

        var captured = new CapturedScreen(display, outputPath, new SizeInt(bitmap.Width, bitmap.Height));
        return Task.FromResult(captured);
    }
}
