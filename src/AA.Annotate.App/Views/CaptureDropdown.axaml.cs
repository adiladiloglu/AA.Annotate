using AA.Annotate.App.ViewModels;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace AA.Annotate.App.Views;

public partial class CaptureDropdown : UserControl
{
    private readonly Dictionary<Button, CaptureViewModel> _items = [];

    public event EventHandler<CaptureViewModel>? CaptureSelected;

    public event EventHandler? NewCaptureRequested;

    public CaptureDropdown()
    {
        InitializeComponent();
        NewCaptureButton.Click += (_, _) => NewCaptureRequested?.Invoke(this, EventArgs.Empty);
    }

    public void SetCaptures(IEnumerable<CaptureViewModel> captures)
    {
        CaptureList.Children.Clear();
        _items.Clear();

        foreach (var capture in captures)
        {
            var button = new Button
            {
                Width = 148,
                Height = 52,
                Padding = new Avalonia.Thickness(5),
                Background = capture.IsSelected
                    ? App.Current?.FindResource("OverlayActiveBrush") as Avalonia.Media.IBrush
                    : App.Current?.FindResource("OverlayRestBrush") as Avalonia.Media.IBrush,
                BorderBrush = App.Current?.FindResource("OverlayBorderBrush") as Avalonia.Media.IBrush,
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(8),
                Content = CreateContent(capture)
            };

            _items[button] = capture;
            button.Click += OnCaptureClicked;
            CaptureList.Children.Add(button);
        }
    }

    private static Control CreateContent(CaptureViewModel capture)
    {
        var image = new Image
        {
            Width = 82,
            Height = 42,
            Stretch = Avalonia.Media.Stretch.UniformToFill,
            Source = File.Exists(capture.ThumbnailPath) ? new Bitmap(capture.ThumbnailPath) : null
        };

        return new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 8,
            Children =
            {
                image,
                new TextBlock
                {
                    Text = capture.Number.ToString(),
                    Foreground = Avalonia.Media.Brushes.White,
                    FontSize = 18,
                    LineHeight = 18,
                    FontWeight = Avalonia.Media.FontWeight.SemiBold,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    TextAlignment = Avalonia.Media.TextAlignment.Center,
                    [Grid.ColumnProperty] = 1
                }
            }
        };
    }

    private void OnCaptureClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && _items.TryGetValue(button, out var capture))
        {
            CaptureSelected?.Invoke(this, capture);
        }
    }
}
