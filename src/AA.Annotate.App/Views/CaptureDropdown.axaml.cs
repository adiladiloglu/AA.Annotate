using AA.Annotate.App.ViewModels;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace AA.Annotate.App.Views;

public partial class CaptureDropdown : UserControl
{
    private readonly Avalonia.Media.IBrush? _panelBrush;
    private readonly Avalonia.Media.IBrush? _itemBrush;
    private readonly Avalonia.Media.IBrush? _selectedItemBrush;
    private readonly Dictionary<Button, CaptureViewModel> _items = [];
    private readonly Dictionary<Button, CaptureViewModel> _deleteItems = [];

    public event EventHandler<CaptureViewModel>? CaptureSelected;

    public event EventHandler<CaptureViewModel>? CaptureDeleteRequested;

    public event EventHandler? NewCaptureRequested;

    public CaptureDropdown()
    {
        InitializeComponent();
        _panelBrush = App.Current?.FindResource("PanelSurfaceBrush") as Avalonia.Media.IBrush;
        _itemBrush = App.Current?.FindResource("PanelItemBrush") as Avalonia.Media.IBrush;
        _selectedItemBrush = App.Current?.FindResource("PanelItemSelectedBrush") as Avalonia.Media.IBrush;
        SetPanelHoverActive(false);
        NewCaptureButton.Click += (_, _) => NewCaptureRequested?.Invoke(this, EventArgs.Empty);
    }

    public void SetPanelHoverActive(bool isActive)
    {
        Opacity = 1;
        RootBorder.Background = _panelBrush;
    }

    public void SetCaptures(IEnumerable<CaptureViewModel> captures)
    {
        CaptureList.Children.Clear();
        _items.Clear();
        _deleteItems.Clear();

        foreach (var capture in captures)
        {
            var button = new Button
            {
                Width = 126,
                Height = 44,
                Padding = new Avalonia.Thickness(4),
                Background = capture.IsSelected
                    ? _selectedItemBrush
                    : _itemBrush,
                BorderBrush = App.Current?.FindResource("OverlayBorderBrush") as Avalonia.Media.IBrush,
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(7),
                Content = CreateContent(capture)
            };

            _items[button] = capture;
            button.Click += OnCaptureClicked;

            var deleteButton = new Button
            {
                Width = 30,
                Height = 44,
                Padding = new Avalonia.Thickness(0),
                Classes = { "iconButton" },
                Content = new PathIcon
                {
                    Width = 15,
                    Height = 15,
                    Data = Avalonia.Media.Geometry.Parse("M8,4 L16,4 L16,6 L21,6 L21,8 L19,8 L18,21 C17.9,22.1 17.1,23 16,23 L8,23 C6.9,23 6.1,22.1 6,21 L5,8 L3,8 L3,6 L8,6 Z M8,8 L8.9,21 L15.1,21 L16,8 Z M10,10 L12,10 L12,19 L10,19 Z M14,10 L16,10 L16,19 L14,19 Z")
                }
            };
            ToolTip.SetTip(deleteButton, "Remove capture");
            _deleteItems[deleteButton] = capture;
            deleteButton.Click += OnCaptureDeleteClicked;

            CaptureList.Children.Add(new Grid
            {
                Width = 162,
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 5,
                Children =
                {
                    button,
                    deleteButton
                }
            });
            Grid.SetColumn(deleteButton, 1);
        }
    }

    public void SetCanCreateCapture(bool canCreate)
    {
        NewCaptureButton.IsEnabled = canCreate;
        NewCaptureButton.Opacity = canCreate ? 1 : 0.45;
        ToolTip.SetTip(
            NewCaptureButton,
            canCreate ? "New capture" : "Finish the current crop or annotation first");
    }

    private static Control CreateContent(CaptureViewModel capture)
    {
        var image = new Image
        {
            Width = 70,
            Height = 34,
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
            ColumnSpacing = 6,
            Children =
            {
                image,
                new TextBlock
                {
                    Text = capture.Number.ToString(),
                    Foreground = Avalonia.Media.Brushes.White,
                    FontSize = 15,
                    LineHeight = 15,
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

    private void OnCaptureDeleteClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && _deleteItems.TryGetValue(button, out var capture))
        {
            CaptureDeleteRequested?.Invoke(this, capture);
        }
    }
}
