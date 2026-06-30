using AA.Annotate.App.ViewModels;
using AA.Annotate.Core.Geometry;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace AA.Annotate.App.Views;

public partial class DisplayDropdown : UserControl
{
    private const int MapWidth = 298;
    private const int MapHeight = 112;
    private const int MapPadding = 8;
    private readonly IBrush? _panelBrush;
    private readonly IBrush? _itemBrush;
    private readonly IBrush? _selectedItemBrush;
    private readonly Dictionary<Button, DisplayViewModel> _items = [];

    public event EventHandler<DisplayViewModel>? DisplaySelected;

    public DisplayDropdown()
    {
        InitializeComponent();
        _panelBrush = App.Current?.FindResource("PanelSurfaceBrush") as IBrush;
        _itemBrush = App.Current?.FindResource("PanelItemBrush") as IBrush;
        _selectedItemBrush = App.Current?.FindResource("PanelItemSelectedBrush") as IBrush;
        SetPanelHoverActive(false);
    }

    public void SetPanelHoverActive(bool isActive)
    {
        Opacity = 1;
        RootBorder.Background = _panelBrush;
    }

    public void SetDisplays(IEnumerable<DisplayViewModel> displays)
    {
        DisplayMap.Children.Clear();
        _items.Clear();
        var displayList = displays.ToList();
        var projected = DisplayLayoutProjector.Project(
            displayList.Select(display => display.Display.Bounds).ToList(),
            new SizeInt(MapWidth, MapHeight),
            MapPadding);

        for (var index = 0; index < displayList.Count; index++)
        {
            var display = displayList[index];
            var bounds = projected[index];
            var button = new Button
            {
                Width = bounds.Width,
                Height = bounds.Height,
                Padding = new Thickness(0),
                Background = display.IsCurrent
                    ? _selectedItemBrush
                    : _itemBrush,
                BorderBrush = App.Current?.FindResource("OverlayBorderBrush") as Avalonia.Media.IBrush,
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(5),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Content = CreateContent(display)
            };

            _items[button] = display;
            button.Click += OnDisplayClicked;
            Canvas.SetLeft(button, bounds.X);
            Canvas.SetTop(button, bounds.Y);
            DisplayMap.Children.Add(button);
        }
    }

    private static Control CreateContent(DisplayViewModel display)
    {
        return new Border
        {
            Child = new TextBlock
            {
                Text = display.Number.ToString(),
                Foreground = Avalonia.Media.Brushes.White,
                FontSize = 19,
                FontWeight = FontWeight.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    private void OnDisplayClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button button && _items.TryGetValue(button, out var display))
        {
            DisplaySelected?.Invoke(this, display);
        }
    }
}
