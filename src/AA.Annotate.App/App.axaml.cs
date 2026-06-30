using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using System.Globalization;

namespace AA.Annotate.App;

public partial class App : Application
{
    public override void Initialize()
    {
        RegisterDesignResources();
        Styles.Add(new FluentTheme());
        RegisterControlStyles();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var args = desktop.Args ?? [];
            desktop.MainWindow = new MainWindow(
                ReadSessionFolder(args),
                ReadSessionRoot(args),
                ReadIdleTimeout(args));
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void RegisterDesignResources()
    {
        Resources["RadiusSmall"] = new CornerRadius(6);
        Resources["RadiusMedium"] = new CornerRadius(8);
        Resources["RadiusLarge"] = new CornerRadius(14);
        Resources["OverlayRestBrush"] = Brush("#5916171A");
        Resources["OverlaySolidBrush"] = Brush("#FF16171A");
        Resources["OverlayHoverBrush"] = Brush("#DCEFF4FF");
        Resources["OverlayActiveBrush"] = Brush("#D42C3440");
        Resources["OverlayBorderBrush"] = Brush("#18FFFFFF");
        Resources["PanelSurfaceBrush"] = Brush("#FF2B2B2B");
        Resources["PanelItemBrush"] = Brush("#FF343434");
        Resources["PanelItemSelectedBrush"] = Brush("#FF3A414B");
        Resources["SystemAccentColor"] = Color.Parse("#DCEFF4FF");
        Resources["SystemAccentColorDark1"] = Color.Parse("#B9D8E5F2");
        Resources["SystemAccentColorDark2"] = Color.Parse("#96B8C7D4");
        Resources["SystemAccentColorDark3"] = Color.Parse("#738C99A6");
        Resources["SystemAccentColorLight1"] = Color.Parse("#EAF7FBFF");
        Resources["SystemAccentColorLight2"] = Color.Parse("#DCEFF4FF");
        Resources["SystemAccentColorLight3"] = Color.Parse("#C8E2EBF6");
        Resources["ConfirmBrush"] = Brush("#2FBC64");
        Resources["AnnotationBrush"] = Brush("#0DE0A536");
        Resources["AnnotationStrokeBrush"] = Brush("#F2E0A536");
        Resources["InvalidAnnotationBrush"] = Brush("#0FEF4444");
        Resources["InvalidAnnotationStrokeBrush"] = Brush("#F2EF4444");
        Resources["CropBrush"] = Brush("#8F2D72D9");
        Resources["CommentEditorBrush"] = Resources["PanelSurfaceBrush"];
        Resources["TextInputBrush"] = Brush("#F20B0D10");
        Resources["TextSelectionBrush"] = Brush("#FF2D72D9");
        Resources["TextSelectionForegroundBrush"] = Brush("#FFFFFFFF");
    }

    private void RegisterControlStyles()
    {
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("iconButton"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Brush("#22FFFFFF")),
                new Setter(Button.BorderBrushProperty, Brush("#40FFFFFF")),
                new Setter(Button.BorderThicknessProperty, new Thickness(1)),
                new Setter(Button.CornerRadiusProperty, new CornerRadius(8)),
                new Setter(Button.ForegroundProperty, Brushes.White),
                new Setter(Button.HorizontalContentAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Center),
                new Setter(Button.VerticalContentAlignmentProperty, Avalonia.Layout.VerticalAlignment.Center),
                new Setter(Button.PaddingProperty, new Thickness(0))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("iconButton").Class(":pointerover"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["OverlayHoverBrush"]),
                new Setter(Button.ForegroundProperty, Brush("#1F2937"))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("activeIconButton"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["OverlayActiveBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["OverlayBorderBrush"]),
                new Setter(Button.BorderThicknessProperty, new Thickness(1)),
                new Setter(Button.ForegroundProperty, Brushes.White),
                new Setter(Button.CornerRadiusProperty, new CornerRadius(8)),
                new Setter(Button.HorizontalContentAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Center),
                new Setter(Button.VerticalContentAlignmentProperty, Avalonia.Layout.VerticalAlignment.Center),
                new Setter(Button.PaddingProperty, new Thickness(10, 0))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("captureActionButton"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Brush("#332C3440")),
                new Setter(Button.BorderBrushProperty, Brushes.Transparent),
                new Setter(Button.BorderThicknessProperty, new Thickness(0)),
                new Setter(Button.ForegroundProperty, Brushes.White),
                new Setter(Button.CornerRadiusProperty, new CornerRadius(8)),
                new Setter(Button.HorizontalContentAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Center),
                new Setter(Button.VerticalContentAlignmentProperty, Avalonia.Layout.VerticalAlignment.Center),
                new Setter(Button.PaddingProperty, new Thickness(0))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("captureActionButton").Class(":pointerover"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["OverlayHoverBrush"]),
                new Setter(Button.ForegroundProperty, Brush("#1F2937"))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("confirmButton"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["ConfirmBrush"]),
                new Setter(Button.BorderBrushProperty, Brush("#72FFFFFF")),
                new Setter(Button.BorderThicknessProperty, new Thickness(1)),
                new Setter(Button.ForegroundProperty, Brushes.White),
                new Setter(Button.CornerRadiusProperty, new CornerRadius(8)),
                new Setter(Button.HorizontalContentAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Center),
                new Setter(Button.VerticalContentAlignmentProperty, Avalonia.Layout.VerticalAlignment.Center),
                new Setter(Button.PaddingProperty, new Thickness(0))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("commentTextButton"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Brushes.Transparent),
                new Setter(Button.BorderBrushProperty, Brushes.Transparent),
                new Setter(Button.BorderThicknessProperty, new Thickness(0)),
                new Setter(Button.ForegroundProperty, Brush("#C8CDD3")),
                new Setter(Button.CornerRadiusProperty, new CornerRadius(7)),
                new Setter(Button.PaddingProperty, new Thickness(10, 0)),
                new Setter(Button.HeightProperty, 30d),
                new Setter(Button.FontSizeProperty, 12d),
                new Setter(Button.HorizontalContentAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Center),
                new Setter(Button.VerticalContentAlignmentProperty, Avalonia.Layout.VerticalAlignment.Center)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("commentTextButton").Class(":pointerover"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Brush("#18FFFFFF")),
                new Setter(Button.ForegroundProperty, Brushes.White)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("commentPrimaryButton"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Brush("#DCE8EAED")),
                new Setter(Button.BorderBrushProperty, Brushes.Transparent),
                new Setter(Button.BorderThicknessProperty, new Thickness(0)),
                new Setter(Button.ForegroundProperty, Brush("#16171A")),
                new Setter(Button.CornerRadiusProperty, new CornerRadius(7)),
                new Setter(Button.PaddingProperty, new Thickness(12, 0)),
                new Setter(Button.HeightProperty, 30d),
                new Setter(Button.FontSizeProperty, 12d),
                new Setter(Button.FontWeightProperty, FontWeight.SemiBold),
                new Setter(Button.HorizontalContentAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Center),
                new Setter(Button.VerticalContentAlignmentProperty, Avalonia.Layout.VerticalAlignment.Center)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("commentPrimaryButton").Class(":pointerover"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Brush("#F5F7FA"))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Border>().Class("commentTextHost"))
        {
            Setters =
            {
                new Setter(Border.BackgroundProperty, Brushes.Transparent),
                new Setter(Border.BorderBrushProperty, Brushes.Transparent),
                new Setter(Border.BorderThicknessProperty, new Thickness(0)),
                new Setter(Border.CornerRadiusProperty, new CornerRadius(0))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Border>().Class("commentTextHost").Class("focused"))
        {
            Setters =
            {
                new Setter(Border.BackgroundProperty, Brushes.Transparent),
                new Setter(Border.BorderBrushProperty, Brushes.Transparent)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<TextBox>().Class("commentTextBox"))
        {
            Setters =
            {
                new Setter(TextBox.BackgroundProperty, Brushes.Transparent),
                new Setter(TextBox.ForegroundProperty, Brushes.White),
                new Setter(TextBox.FocusAdornerProperty, null),
                new Setter(TextBox.CaretBrushProperty, Brushes.White),
                new Setter(TextBox.SelectionBrushProperty, Resources["TextSelectionBrush"]),
                new Setter(TextBox.SelectionForegroundBrushProperty, Resources["TextSelectionForegroundBrush"]),
                new Setter(TextBox.BorderBrushProperty, Brushes.Transparent),
                new Setter(TextBox.BorderThicknessProperty, new Thickness(0)),
                new Setter(TextBox.CornerRadiusProperty, new CornerRadius(0)),
                new Setter(TextBox.PaddingProperty, new Thickness(0)),
                new Setter(TextBox.FontSizeProperty, 13d),
                new Setter(TextBox.LineHeightProperty, 20d)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<TextBox>().Class("commentTextBox").Class(":focus"))
        {
            Setters =
            {
                new Setter(TextBox.BackgroundProperty, Brushes.Transparent),
                new Setter(TextBox.BorderBrushProperty, Brushes.Transparent),
                new Setter(TextBox.BorderThicknessProperty, new Thickness(0))
            }
        });
    }

    private static SolidColorBrush Brush(string color)
    {
        return new SolidColorBrush(Color.Parse(color));
    }

    internal static string? ReadSessionFolder(IReadOnlyList<string> args)
    {
        return ReadOption(args, "--session");
    }

    internal static string? ReadSessionRoot(IReadOnlyList<string> args)
    {
        return ReadOption(args, "--session-root");
    }

    internal static TimeSpan? ReadIdleTimeout(IReadOnlyList<string> args)
    {
        var value = ReadOption(args, "--idle-timeout-seconds");
        if (!string.IsNullOrWhiteSpace(value))
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && seconds > 0
                ? TimeSpan.FromSeconds(seconds)
                : null;
        }

        return null;
    }

    private static string? ReadOption(IReadOnlyList<string> args, string name)
    {
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }
}
