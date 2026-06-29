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
            desktop.MainWindow = new MainWindow(ReadSessionFolder(args), ReadIdleTimeout(args));
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void RegisterDesignResources()
    {
        Resources["RadiusSmall"] = new CornerRadius(6);
        Resources["RadiusMedium"] = new CornerRadius(8);
        Resources["RadiusLarge"] = new CornerRadius(14);
        Resources["OverlayRestBrush"] = Brush("#59717A86");
        Resources["OverlayHoverBrush"] = Brush("#DCEFF4FF");
        Resources["OverlayActiveBrush"] = Brush("#D42C3440");
        Resources["OverlayBorderBrush"] = Brush("#72FFFFFF");
        Resources["SystemAccentColor"] = Color.Parse("#DCEFF4FF");
        Resources["SystemAccentColorDark1"] = Color.Parse("#B9D8E5F2");
        Resources["SystemAccentColorDark2"] = Color.Parse("#96B8C7D4");
        Resources["SystemAccentColorDark3"] = Color.Parse("#738C99A6");
        Resources["SystemAccentColorLight1"] = Color.Parse("#EAF7FBFF");
        Resources["SystemAccentColorLight2"] = Color.Parse("#DCEFF4FF");
        Resources["SystemAccentColorLight3"] = Color.Parse("#C8E2EBF6");
        Resources["ConfirmBrush"] = Brush("#2FBC64");
        Resources["AnnotationBrush"] = Brush("#52F6A609");
        Resources["AnnotationStrokeBrush"] = Brush("#B8F6A609");
        Resources["CropBrush"] = Brush("#8F2D72D9");
        Resources["CommentEditorBrush"] = Brush("#D42C3440");
        Resources["TextInputBrush"] = Brush("#8F111827");
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
        Styles.Add(new Style(selector => selector.OfType<Border>().Class("commentTextHost"))
        {
            Setters =
            {
                new Setter(Border.BackgroundProperty, Resources["TextInputBrush"]),
                new Setter(Border.BorderBrushProperty, Resources["OverlayBorderBrush"]),
                new Setter(Border.BorderThicknessProperty, new Thickness(1)),
                new Setter(Border.CornerRadiusProperty, new CornerRadius(6))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Border>().Class("commentTextHost").Class("focused"))
        {
            Setters =
            {
                new Setter(Border.BackgroundProperty, Resources["OverlayActiveBrush"]),
                new Setter(Border.BorderBrushProperty, Resources["OverlayHoverBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<TextBox>().Class("commentTextBox"))
        {
            Setters =
            {
                new Setter(TextBox.BackgroundProperty, Brushes.Transparent),
                new Setter(TextBox.ForegroundProperty, Brushes.White),
                new Setter(TextBox.BorderBrushProperty, Brushes.Transparent),
                new Setter(TextBox.BorderThicknessProperty, new Thickness(0)),
                new Setter(TextBox.CornerRadiusProperty, new CornerRadius(0)),
                new Setter(TextBox.PaddingProperty, new Thickness(10, 7))
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
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (string.Equals(args[index], "--session", StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }

    internal static TimeSpan? ReadIdleTimeout(IReadOnlyList<string> args)
    {
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (string.Equals(args[index], "--idle-timeout-seconds", StringComparison.OrdinalIgnoreCase))
            {
                return double.TryParse(args[index + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && seconds > 0
                    ? TimeSpan.FromSeconds(seconds)
                    : null;
            }
        }

        return null;
    }
}
