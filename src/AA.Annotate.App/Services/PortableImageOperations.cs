using AA.Annotate.Core.Geometry;
using Avalonia.Platform;
using SkiaSharp;

namespace AA.Annotate.App.Services;

internal static class PortableImageOperations
{
    private static readonly Uri BoldInterFontUri =
        new("avares://Avalonia.Fonts.Inter/Assets/Inter-Bold.ttf");

    private static readonly Lazy<SKTypeface> BoldInterTypeface =
        new(LoadBoldInterTypeface, LazyThreadSafetyMode.ExecutionAndPublication);

    public static SKBitmap Load(string path)
    {
        var bitmap = SKBitmap.Decode(path);
        return bitmap ?? throw new InvalidDataException($"The image could not be decoded: {path}");
    }

    public static SKBitmap Clone(SKBitmap source)
    {
        var clone = new SKBitmap(source.Info);
        using var canvas = new SKCanvas(clone);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(source, 0, 0);
        return clone;
    }

    public static SKBitmap Resize(SKBitmap source, int width, int height)
    {
        var info = new SKImageInfo(
            Math.Max(1, width),
            Math.Max(1, height),
            SKColorType.Bgra8888,
            SKAlphaType.Premul);
        var resized = source.Resize(info, SKFilterQuality.High);
        return resized ?? throw new InvalidOperationException(
            $"The image could not be resized to {info.Width}x{info.Height}.");
    }

    public static void WriteCrop(SKBitmap source, RectInt crop, string destinationPath)
    {
        var target = new SKBitmap(
            new SKImageInfo(crop.Width, crop.Height, SKColorType.Bgra8888, SKAlphaType.Premul));
        try
        {
            using var canvas = new SKCanvas(target);
            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(
                source,
                ToSkRect(crop),
                SKRect.Create(0, 0, crop.Width, crop.Height));
            SavePng(target, destinationPath);
        }
        finally
        {
            target.Dispose();
        }
    }

    public static MemoryStream CreateBlurredPreview(string path, int blurScale)
    {
        using var source = Load(path);
        var smallWidth = Math.Max(1, source.Width / Math.Max(1, blurScale));
        var smallHeight = Math.Max(1, source.Height / Math.Max(1, blurScale));
        using var small = Resize(source, smallWidth, smallHeight);
        using var blurred = Resize(small, source.Width, source.Height);
        return EncodePng(blurred);
    }

    public static void SavePng(SKBitmap bitmap, string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = File.Create(path);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        data.SaveTo(stream);
    }

    public static SKPaint CreateTextPaint(float size, SKColor color)
    {
        return new SKPaint
        {
            Color = color,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            TextSize = size,
            Typeface = BoldInterTypeface.Value
        };
    }

    public static SKRect ToSkRect(RectInt rect)
    {
        return SKRect.Create(rect.X, rect.Y, rect.Width, rect.Height);
    }

    private static MemoryStream EncodePng(SKBitmap bitmap)
    {
        var stream = new MemoryStream();
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        data.SaveTo(stream);
        stream.Position = 0;
        return stream;
    }

    private static SKTypeface LoadBoldInterTypeface()
    {
        using var stream = new StandardAssetLoader().Open(BoldInterFontUri);
        return SKTypeface.FromStream(stream) ?? throw new InvalidOperationException(
            $"The bundled Inter font could not be loaded from '{BoldInterFontUri}'.");
    }
}
