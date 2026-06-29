using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace AA.Annotate.App.Views;

public partial class ScreenshotSurface : UserControl
{
    public ScreenshotSurface()
    {
        InitializeComponent();
    }

    public void SetImage(string? path)
    {
        ScreenshotImage.Source = string.IsNullOrWhiteSpace(path) || !File.Exists(path)
            ? null
            : new Bitmap(path);
    }
}
