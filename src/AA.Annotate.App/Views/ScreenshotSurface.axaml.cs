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
        Bitmap? next = null;
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            using var stream = File.OpenRead(path);
            next = new Bitmap(stream);
        }

        var previous = ScreenshotImage.Source as IDisposable;
        ScreenshotImage.Source = next;
        previous?.Dispose();
    }
}
