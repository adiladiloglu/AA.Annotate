using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using AA.Annotate.Core.Geometry;
using AA.Annotate.Platform;

namespace AA.Annotate.Platform.Windows;

[SupportedOSPlatform("windows6.1")]
public sealed class WindowsScreenCaptureService : IScreenCaptureService
{
    public Task<ScreenCaptureResult> CaptureScreenAsync(ScreenCaptureRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            request.CancellationToken.ThrowIfCancellationRequested();
            if (request.PreferredDisplay is not { } display)
            {
                return Task.FromResult(ScreenCaptureResult.Unavailable(
                    "Windows screen capture requires a preferred display."));
            }

            if (request.IncludeCursor)
            {
                return Task.FromResult(ScreenCaptureResult.Unavailable(
                    "Windows screen capture does not support including the cursor."));
            }

            Directory.CreateDirectory(Path.GetDirectoryName(request.DestinationPath) ?? ".");

            using var bitmap = new Bitmap(display.Bounds.Width, display.Bounds.Height);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(
                display.Bounds.X,
                display.Bounds.Y,
                0,
                0,
                new Size(display.Bounds.Width, display.Bounds.Height),
                CopyPixelOperation.SourceCopy);
            request.CancellationToken.ThrowIfCancellationRequested();
            bitmap.Save(request.DestinationPath, ImageFormat.Png);

            var captured = new CapturedScreen(
                display,
                request.DestinationPath,
                new SizeInt(bitmap.Width, bitmap.Height));
            return Task.FromResult(ScreenCaptureResult.Completed(captured));
        }
        catch (OperationCanceledException)
        {
            TryDeletePartialCapture(request.DestinationPath);
            return Task.FromResult(ScreenCaptureResult.Cancelled());
        }
        catch (PlatformNotSupportedException exception)
        {
            TryDeletePartialCapture(request.DestinationPath);
            return Task.FromResult(ScreenCaptureResult.Unavailable(exception.Message));
        }
        catch (Exception exception)
        {
            TryDeletePartialCapture(request.DestinationPath);
            return Task.FromResult(ScreenCaptureResult.Failed(exception.Message));
        }
    }

    private static void TryDeletePartialCapture(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
