using System.Runtime.InteropServices;
using AA.Annotate.Core.Geometry;
using AA.Annotate.Core.Services;
using AA.Annotate.Platform;
using SkiaSharp;

namespace AA.Annotate.Platform.Linux;

public sealed class LinuxX11ScreenCaptureService : IScreenCaptureService
{
    public async Task<ScreenCaptureResult> CaptureScreenAsync(ScreenCaptureRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        nint displayConnection = 0;
        nint nativeImage = 0;
        var partialPath = $"{request.DestinationPath}.{Guid.NewGuid():N}.partial";

        try
        {
            request.CancellationToken.ThrowIfCancellationRequested();

            if (!OperatingSystem.IsLinux())
            {
                return ScreenCaptureResult.Unavailable("X11 screen capture is available only on Linux.");
            }

            if (request.PreferredDisplay is not { } display)
            {
                return ScreenCaptureResult.Unavailable("X11 screen capture requires a preferred display.");
            }

            if (request.IncludeCursor)
            {
                return ScreenCaptureResult.Unavailable(
                    "X11 screen capture does not support including the cursor.");
            }

            if (display.Bounds.Width <= 0 || display.Bounds.Height <= 0)
            {
                return ScreenCaptureResult.DisplayDisconnected(
                    "The requested X11 display has invalid dimensions.");
            }

            var displayName = Environment.GetEnvironmentVariable("DISPLAY");
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return ScreenCaptureResult.Unavailable(
                    "DISPLAY is not set, so an X11 server cannot be reached.");
            }

            displayConnection = LinuxX11Native.OpenDisplay(displayName);
            if (displayConnection == 0)
            {
                return ScreenCaptureResult.Unavailable(
                    $"Unable to connect to the X11 display '{displayName}'.");
            }

            var screen = LinuxX11Native.DefaultScreen(displayConnection);
            var root = LinuxX11Native.RootWindow(displayConnection, screen);
            if (root == 0 || LinuxX11Native.GetGeometry(
                    displayConnection,
                    root,
                    out _,
                    out _,
                    out _,
                    out var rootWidth,
                    out var rootHeight,
                    out _,
                    out _) == 0)
            {
                return ScreenCaptureResult.DisplayDisconnected(
                    "The X11 root window is no longer available.");
            }

            if (!FitsWithinRoot(display.Bounds, rootWidth, rootHeight))
            {
                return ScreenCaptureResult.DisplayDisconnected(
                    "The requested display bounds are outside the current X11 root window.");
            }

            request.CancellationToken.ThrowIfCancellationRequested();
            nativeImage = LinuxX11Native.GetImage(
                displayConnection,
                root,
                display.Bounds.X,
                display.Bounds.Y,
                checked((uint)display.Bounds.Width),
                checked((uint)display.Bounds.Height),
                LinuxX11Native.AllPlanes,
                LinuxX11Native.ZPixmap);
            if (nativeImage == 0)
            {
                return ScreenCaptureResult.DisplayDisconnected(
                    "X11 could not capture the requested display.");
            }

            var xImage = Marshal.PtrToStructure<XImage>(nativeImage);
            var rawLength = checked(xImage.BytesPerLine * xImage.Height);
            if (xImage.Data == 0 || rawLength <= 0)
            {
                return ScreenCaptureResult.Failed("X11 returned an empty screen image.");
            }

            var rawPixels = new byte[rawLength];
            Marshal.Copy(xImage.Data, rawPixels, 0, rawPixels.Length);
            var bgraPixels = X11PixelConverter.ConvertToBgra(new X11ImageData(
                xImage.Width,
                xImage.Height,
                xImage.XOffset,
                xImage.ByteOrder,
                xImage.BytesPerLine,
                xImage.BitsPerPixel,
                xImage.RedMask,
                xImage.GreenMask,
                xImage.BlueMask,
                rawPixels));

            request.CancellationToken.ThrowIfCancellationRequested();
            var png = EncodePng(xImage.Width, xImage.Height, bgraPixels);
            request.CancellationToken.ThrowIfCancellationRequested();

            Directory.CreateDirectory(Path.GetDirectoryName(request.DestinationPath) ?? ".");
            await File.WriteAllBytesAsync(
                partialPath,
                png,
                request.CancellationToken).ConfigureAwait(false);
            PrivateFileSystem.ProtectFile(partialPath);
            request.CancellationToken.ThrowIfCancellationRequested();
            File.Move(partialPath, request.DestinationPath, overwrite: true);
            PrivateFileSystem.ProtectFile(request.DestinationPath);

            var captured = new CapturedScreen(
                display,
                request.DestinationPath,
                new SizeInt(xImage.Width, xImage.Height));
            return ScreenCaptureResult.Completed(captured);
        }
        catch (OperationCanceledException)
        {
            return ScreenCaptureResult.Cancelled();
        }
        catch (DllNotFoundException)
        {
            return ScreenCaptureResult.Unavailable(
                "The X11 client library is not installed.");
        }
        catch (EntryPointNotFoundException)
        {
            return ScreenCaptureResult.Unavailable(
                "The installed X11 client library is incompatible.");
        }
        catch (Exception exception)
        {
            return ScreenCaptureResult.Failed(exception.Message);
        }
        finally
        {
            TryDeletePartialCapture(partialPath);

            if (nativeImage != 0)
            {
                _ = LinuxX11Native.DestroyImage(nativeImage);
            }

            if (displayConnection != 0)
            {
                _ = LinuxX11Native.CloseDisplay(displayConnection);
            }
        }
    }

    private static bool FitsWithinRoot(RectInt bounds, uint rootWidth, uint rootHeight)
    {
        if (bounds.X < 0 || bounds.Y < 0)
        {
            return false;
        }

        var right = (long)bounds.X + bounds.Width;
        var bottom = (long)bounds.Y + bounds.Height;
        return right <= rootWidth && bottom <= rootHeight;
    }

    private static byte[] EncodePng(int width, int height, byte[] bgraPixels)
    {
        var imageInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);
        using var bitmap = new SKBitmap(imageInfo);
        Marshal.Copy(bgraPixels, 0, bitmap.GetPixels(), bgraPixels.Length);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, quality: 100);
        return encoded?.ToArray()
            ?? throw new InvalidOperationException("Skia could not encode the X11 capture as PNG.");
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
