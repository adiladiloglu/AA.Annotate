using System.Collections.ObjectModel;
using AA.Annotate.Core.Geometry;
using AA.Annotate.Platform;

namespace AA.Annotate.App.ViewModels;

public sealed class CaptureViewModel
{
    public CaptureViewModel(
        string captureId,
        int number,
        DisplayDescriptor display,
        string screenshotPath,
        string thumbnailPath,
        SizeInt screenshotPixelSize,
        RectInt screenBounds,
        bool isSelected)
    {
        CaptureId = captureId;
        Number = number;
        Display = display;
        ScreenshotPath = screenshotPath;
        ThumbnailPath = thumbnailPath;
        ScreenshotPixelSize = screenshotPixelSize;
        ScreenBounds = screenBounds;
        CropPixelRect = new RectInt(0, 0, screenshotPixelSize.Width, screenshotPixelSize.Height);
        CropRect = CropPixelRect;
        ViewportSize = screenshotPixelSize;
        IsSelected = isSelected;
    }

    public string CaptureId { get; }

    public int Number { get; }

    public DisplayDescriptor Display { get; }

    public string ScreenshotPath { get; }

    public string ThumbnailPath { get; }

    public SizeInt ScreenshotPixelSize { get; }

    public RectInt ScreenBounds { get; }

    public SizeInt ViewportSize { get; set; }

    public RectInt CropPixelRect { get; set; }

    public RectInt CropRect { get; set; }

    public bool IsSelected { get; set; }

    public ObservableCollection<AnnotationViewModel> Annotations { get; } = [];

    public int GetNextAnnotationNumber()
    {
        return Annotations.Count == 0 ? 1 : Annotations.Max(annotation => annotation.Number) + 1;
    }
}
