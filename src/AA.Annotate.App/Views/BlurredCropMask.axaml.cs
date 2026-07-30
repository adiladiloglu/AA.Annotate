using AA.Annotate.App.Services;
using AA.Annotate.Core.Geometry;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace AA.Annotate.App.Views;

public partial class BlurredCropMask : UserControl
{
    private const int BlurScale = 14;
    private RectInt? _crop;
    private Bitmap? _source;

    public BlurredCropMask()
    {
        InitializeComponent();
        SizeChanged += (_, _) => UpdateRegions();
    }

    public void SetImage(string? path)
    {
        var source = CreateBlurredSource(path);

        foreach (var image in GetImages())
        {
            image.Source = source;
            image.Opacity = 0.95;
        }

        _source?.Dispose();
        _source = source;
    }

    private static Bitmap? CreateBlurredSource(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        using var stream = PortableImageOperations.CreateBlurredPreview(path, BlurScale);
        return new Bitmap(stream);
    }

    public void SetCrop(RectInt crop)
    {
        _crop = crop;
        UpdateRegions();
    }

    private void UpdateRegions()
    {
        if (_crop is not { } crop || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        var bounds = new Rect(0, 0, Bounds.Width, Bounds.Height);
        var cropRect = new Rect(
            Math.Clamp(crop.X, 0, bounds.Width),
            Math.Clamp(crop.Y, 0, bounds.Height),
            Math.Clamp(crop.Width, 0, bounds.Width),
            Math.Clamp(crop.Height, 0, bounds.Height));

        var rightX = Math.Min(bounds.Width, cropRect.X + cropRect.Width);
        var bottomY = Math.Min(bounds.Height, cropRect.Y + cropRect.Height);

        SetRegion(TopRegion, TopImage, 0, 0, bounds.Width, cropRect.Y);
        SetRegion(LeftRegion, LeftImage, 0, cropRect.Y, cropRect.X, cropRect.Height);
        SetRegion(RightRegion, RightImage, rightX, cropRect.Y, bounds.Width - rightX, cropRect.Height);
        SetRegion(BottomRegion, BottomImage, 0, bottomY, bounds.Width, bounds.Height - bottomY);
    }

    private void SetRegion(Border region, Image image, double x, double y, double width, double height)
    {
        var visible = width > 0.5 && height > 0.5;
        region.IsVisible = visible;
        if (!visible)
        {
            return;
        }

        Canvas.SetLeft(region, x);
        Canvas.SetTop(region, y);
        region.Width = width;
        region.Height = height;

        image.Width = Bounds.Width;
        image.Height = Bounds.Height;
        Canvas.SetLeft(image, -x);
        Canvas.SetTop(image, -y);
    }

    private IEnumerable<Image> GetImages()
    {
        yield return TopImage;
        yield return LeftImage;
        yield return RightImage;
        yield return BottomImage;
    }
}
