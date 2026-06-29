using AA.Annotate.App.ViewModels;
using AA.Annotate.Core.Geometry;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace AA.Annotate.App.Views;

public partial class DisplayDropdown : UserControl
{
    private const int MapWidth = 340;
    private const int MapHeight = 130;
    private const int MapPadding = 10;
    private readonly Dictionary<Button, DisplayViewModel> _items = [];

    public event EventHandler<DisplayViewModel>? DisplaySelected;

    public DisplayDropdown()
    {
        InitializeComponent();
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
                    ? App.Current?.FindResource("OverlayActiveBrush") as Avalonia.Media.IBrush
                    : new SolidColorBrush(Color.FromArgb(150, 42, 45, 48)),
                BorderBrush = App.Current?.FindResource("OverlayBorderBrush") as Avalonia.Media.IBrush,
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(4),
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
                FontSize = 24,
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
