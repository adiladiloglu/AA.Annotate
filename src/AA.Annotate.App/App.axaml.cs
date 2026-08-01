using AA.Annotate.App.Services;
using AA.Annotate.App.Styling;
using AA.Annotate.Core.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using System.Globalization;

namespace AA.Annotate.App;

public partial class App : Application
{
    private static readonly TimeSpan DefaultIdleTimeout = TimeSpan.FromMinutes(1);

    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Dark;
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
                ReadExportFolder(args),
                ReadSessionRoot(args),
                ReadExportRoot(args),
                ReadIdleTimeout(args),
                ReadDefaultScale(args),
                ReadCaller(args));
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void RegisterDesignResources()
    {
        Resources["RadiusSmall"] = new CornerRadius(6);
        Resources["RadiusMedium"] = new CornerRadius(8);
        Resources["RadiusLarge"] = new CornerRadius(12);
        Resources["OverlayRestBrush"] = Brush("#F21B2227");
        Resources["OverlaySolidBrush"] = Brush("#FF171D21");
        Resources["OverlayHoverBrush"] = Brush("#FF34434B");
        Resources["OverlayActiveBrush"] = Brush("#FF2D3A40");
        Resources["OverlayBorderBrush"] = Brush("#66778992");
        Resources["PanelSurfaceBrush"] = Brush("#FF20282D");
        Resources["PanelItemBrush"] = Brush(InteractionPalette.PanelItem);
        Resources["PanelItemHoverBrush"] = Brush(InteractionPalette.PanelItemHover);
        Resources["PanelItemPressedBrush"] = Brush(InteractionPalette.PanelItemPressed);
        Resources["PanelItemSelectedBrush"] = Brush(InteractionPalette.PanelItemSelected);
        Resources["PanelItemSelectedHoverBrush"] = Brush(InteractionPalette.PanelItemSelectedHover);
        Resources["PanelItemSelectedPressedBrush"] = Brush(InteractionPalette.PanelItemSelectedPressed);
        Resources["PrimaryTextBrush"] = Brush(InteractionPalette.PrimaryText);
        Resources["SecondaryTextBrush"] = Brush(InteractionPalette.SecondaryText);
        Resources["MutedTextBrush"] = Brush(InteractionPalette.MutedText);
        Resources["ControlHoverBrush"] = Brush(InteractionPalette.ControlHover);
        Resources["ControlPressedBrush"] = Brush(InteractionPalette.ControlPressed);
        Resources["ControlHoverBorderBrush"] = Brush(InteractionPalette.ControlHoverBorder);
        Resources["ActiveControlBrush"] = Brush(InteractionPalette.ActiveControl);
        Resources["ActiveControlHoverBrush"] = Brush(InteractionPalette.ActiveControlHover);
        Resources["ActiveControlPressedBrush"] = Brush(InteractionPalette.ActiveControlPressed);
        Resources["ActiveControlForegroundBrush"] = Brush(InteractionPalette.ActiveControlForeground);
        Resources["CaptureSelectorBrush"] = Brush("#FF2C383E");
        Resources["CaptureSelectorBorderBrush"] = Brush("#FF52636B");
        Resources["ToolbarDividerBrush"] = Brush("#66778992");
        Resources["GripBrush"] = Brush("#FF7FE1D3");
        Resources["GripBackgroundBrush"] = Brush("#263D9F94");
        Resources["DangerBrush"] = Brush(InteractionPalette.Danger);
        Resources["DangerHoverBrush"] = Brush(InteractionPalette.DangerHover);
        Resources["DangerPressedBrush"] = Brush(InteractionPalette.DangerPressed);
        Resources["SystemAccentColor"] = Color.Parse("#63D6C7");
        Resources["SystemAccentColorDark1"] = Color.Parse("#42B9AB");
        Resources["SystemAccentColorDark2"] = Color.Parse("#2D9187");
        Resources["SystemAccentColorDark3"] = Color.Parse("#246E68");
        Resources["SystemAccentColorLight1"] = Color.Parse("#D8FAF5");
        Resources["SystemAccentColorLight2"] = Color.Parse("#9DEADE");
        Resources["SystemAccentColorLight3"] = Color.Parse("#7FE1D3");
        Resources["ConfirmBrush"] = Brush(InteractionPalette.Confirm);
        Resources["ConfirmHoverBrush"] = Brush(InteractionPalette.ConfirmHover);
        Resources["ConfirmPressedBrush"] = Brush(InteractionPalette.ConfirmPressed);
        Resources["ConfirmSurfaceBrush"] = Brush(InteractionPalette.ConfirmSurface);
        Resources["ConfirmHoverSurfaceBrush"] = Brush(InteractionPalette.ConfirmHoverSurface);
        Resources["ConfirmPressedSurfaceBrush"] = Brush(InteractionPalette.ConfirmPressedSurface);
        Resources["AnnotationBrush"] = Brush("#0DFFBC57");
        Resources["AnnotationStrokeBrush"] = Brush("#F2FFBC57");
        Resources["InvalidAnnotationBrush"] = Brush("#0FFF6B76");
        Resources["InvalidAnnotationStrokeBrush"] = Brush("#F2FF6B76");
        Resources["CropBrush"] = Brush("#8F62A8FF");
        Resources["CommentEditorBrush"] = Resources["PanelSurfaceBrush"];
        Resources["TextInputBrush"] = Brush("#FF11171A");
        Resources["TextSelectionBrush"] = Brush("#FF2D9187");
        Resources["TextSelectionForegroundBrush"] = Brush("#FFFFFFFF");
    }

    private void RegisterControlStyles()
    {
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("iconButton"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Brushes.Transparent),
                new Setter(Button.BorderBrushProperty, Brushes.Transparent),
                new Setter(Button.BorderThicknessProperty, new Thickness(0)),
                new Setter(Button.CornerRadiusProperty, new CornerRadius(8)),
                new Setter(Button.ForegroundProperty, Resources["PrimaryTextBrush"]),
                new Setter(Button.HorizontalContentAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Center),
                new Setter(Button.VerticalContentAlignmentProperty, Avalonia.Layout.VerticalAlignment.Center),
                new Setter(Button.PaddingProperty, new Thickness(0))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("iconButton").PropertyEquals(InputElement.IsPointerOverProperty, true))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["ControlHoverBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["ControlHoverBorderBrush"]),
                new Setter(Button.BorderThicknessProperty, new Thickness(1)),
                new Setter(Button.ForegroundProperty, Resources["PrimaryTextBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("iconButton").PropertyEquals(Button.IsPressedProperty, true))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["ControlPressedBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["ControlHoverBorderBrush"]),
                new Setter(Button.BorderThicknessProperty, new Thickness(1)),
                new Setter(Button.ForegroundProperty, Resources["PrimaryTextBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("activeIconButton"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["ActiveControlBrush"]),
                new Setter(Button.BorderBrushProperty, Brushes.Transparent),
                new Setter(Button.BorderThicknessProperty, new Thickness(0)),
                new Setter(Button.ForegroundProperty, Resources["ActiveControlForegroundBrush"]),
                new Setter(Button.CornerRadiusProperty, new CornerRadius(8)),
                new Setter(Button.HorizontalContentAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Center),
                new Setter(Button.VerticalContentAlignmentProperty, Avalonia.Layout.VerticalAlignment.Center),
                new Setter(Button.PaddingProperty, new Thickness(8, 0))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("activeIconButton").PropertyEquals(InputElement.IsPointerOverProperty, true))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["ActiveControlHoverBrush"]),
                new Setter(Button.ForegroundProperty, Resources["ActiveControlForegroundBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("activeIconButton").PropertyEquals(Button.IsPressedProperty, true))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["ActiveControlPressedBrush"]),
                new Setter(Button.ForegroundProperty, Resources["ActiveControlForegroundBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("captureSelectorButton"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["CaptureSelectorBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["CaptureSelectorBorderBrush"]),
                new Setter(Button.BorderThicknessProperty, new Thickness(1)),
                new Setter(Button.ForegroundProperty, Resources["PrimaryTextBrush"]),
                new Setter(Button.CornerRadiusProperty, new CornerRadius(8)),
                new Setter(Button.HorizontalContentAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Center),
                new Setter(Button.VerticalContentAlignmentProperty, Avalonia.Layout.VerticalAlignment.Center),
                new Setter(Button.PaddingProperty, new Thickness(8, 0))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("captureSelectorButton").PropertyEquals(InputElement.IsPointerOverProperty, true))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["ControlHoverBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["ControlHoverBorderBrush"]),
                new Setter(Button.ForegroundProperty, Resources["PrimaryTextBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("captureSelectorButton").PropertyEquals(Button.IsPressedProperty, true))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["ControlPressedBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["ControlHoverBorderBrush"]),
                new Setter(Button.ForegroundProperty, Resources["PrimaryTextBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("scalePresetButton"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["PanelItemBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["OverlayBorderBrush"]),
                new Setter(Button.BorderThicknessProperty, new Thickness(1)),
                new Setter(Button.ForegroundProperty, Resources["PrimaryTextBrush"]),
                new Setter(Button.CornerRadiusProperty, new CornerRadius(6)),
                new Setter(Button.WidthProperty, 78d),
                new Setter(Button.MinWidthProperty, 78d),
                new Setter(Button.HeightProperty, 26d),
                new Setter(Button.PaddingProperty, new Thickness(10, 0)),
                new Setter(Button.FontSizeProperty, 12d),
                new Setter(Button.HorizontalAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Stretch),
                new Setter(Button.HorizontalContentAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Center),
                new Setter(Button.VerticalContentAlignmentProperty, Avalonia.Layout.VerticalAlignment.Center)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("scalePresetButton").PropertyEquals(InputElement.IsPointerOverProperty, true))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["PanelItemHoverBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["ControlHoverBorderBrush"]),
                new Setter(Button.ForegroundProperty, Resources["PrimaryTextBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("scalePresetButton").PropertyEquals(Button.IsPressedProperty, true))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["PanelItemPressedBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["ControlHoverBorderBrush"]),
                new Setter(Button.ForegroundProperty, Resources["PrimaryTextBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<TextBox>().Class("scaleTextBox"))
        {
            Setters =
            {
                new Setter(TextBox.BackgroundProperty, Brushes.Transparent),
                new Setter(TextBox.BorderBrushProperty, Brushes.Transparent),
                new Setter(TextBox.BorderThicknessProperty, new Thickness(0)),
                new Setter(TextBox.CornerRadiusProperty, new CornerRadius(0)),
                new Setter(TextBox.FocusAdornerProperty, null),
                new Setter(TextBox.ForegroundProperty, Resources["PrimaryTextBrush"]),
                new Setter(TextBox.CaretBrushProperty, Resources["PrimaryTextBrush"]),
                new Setter(TextBox.SelectionBrushProperty, Resources["TextSelectionBrush"]),
                new Setter(TextBox.SelectionForegroundBrushProperty, Resources["TextSelectionForegroundBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<TextBox>().Class("scaleTextBox").PropertyEquals(InputElement.IsPointerOverProperty, true))
        {
            Setters =
            {
                new Setter(TextBox.BackgroundProperty, Brushes.Transparent),
                new Setter(TextBox.BorderBrushProperty, Brushes.Transparent),
                new Setter(TextBox.BorderThicknessProperty, new Thickness(0))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<TextBox>().Class("scaleTextBox").PropertyEquals(InputElement.IsFocusedProperty, true))
        {
            Setters =
            {
                new Setter(TextBox.BackgroundProperty, Brushes.Transparent),
                new Setter(TextBox.BorderBrushProperty, Brushes.Transparent),
                new Setter(TextBox.BorderThicknessProperty, new Thickness(0))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("panelItemButton"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["PanelItemBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["OverlayBorderBrush"]),
                new Setter(Button.BorderThicknessProperty, new Thickness(1)),
                new Setter(Button.ForegroundProperty, Resources["PrimaryTextBrush"]),
                new Setter(Button.CornerRadiusProperty, new CornerRadius(7))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("panelItemButton").PropertyEquals(InputElement.IsPointerOverProperty, true))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["PanelItemHoverBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["ControlHoverBorderBrush"]),
                new Setter(Button.ForegroundProperty, Resources["PrimaryTextBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("panelItemButton").PropertyEquals(Button.IsPressedProperty, true))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["PanelItemPressedBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["ControlHoverBorderBrush"]),
                new Setter(Button.ForegroundProperty, Resources["PrimaryTextBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("panelItemButton").Class("selectedPanelItemButton"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["PanelItemSelectedBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["GripBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("panelItemButton").Class("selectedPanelItemButton").PropertyEquals(InputElement.IsPointerOverProperty, true))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["PanelItemSelectedHoverBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["ControlHoverBorderBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("panelItemButton").Class("selectedPanelItemButton").PropertyEquals(Button.IsPressedProperty, true))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["PanelItemSelectedPressedBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["ControlHoverBorderBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("captureActionButton"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Brushes.Transparent),
                new Setter(Button.BorderBrushProperty, Brushes.Transparent),
                new Setter(Button.BorderThicknessProperty, new Thickness(0)),
                new Setter(Button.ForegroundProperty, Resources["PrimaryTextBrush"]),
                new Setter(Button.CornerRadiusProperty, new CornerRadius(8)),
                new Setter(Button.HorizontalContentAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Center),
                new Setter(Button.VerticalContentAlignmentProperty, Avalonia.Layout.VerticalAlignment.Center),
                new Setter(Button.PaddingProperty, new Thickness(0))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("captureActionButton").PropertyEquals(InputElement.IsPointerOverProperty, true))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["ControlHoverBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["ControlHoverBorderBrush"]),
                new Setter(Button.BorderThicknessProperty, new Thickness(1)),
                new Setter(Button.ForegroundProperty, Resources["PrimaryTextBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("captureActionButton").PropertyEquals(Button.IsPressedProperty, true))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["ControlPressedBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["ControlHoverBorderBrush"]),
                new Setter(Button.BorderThicknessProperty, new Thickness(1)),
                new Setter(Button.ForegroundProperty, Resources["PrimaryTextBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("confirmButton"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["ConfirmSurfaceBrush"]),
                new Setter(Button.BorderBrushProperty, Brushes.Transparent),
                new Setter(Button.BorderThicknessProperty, new Thickness(0)),
                new Setter(Button.ForegroundProperty, Resources["ConfirmBrush"]),
                new Setter(Button.CornerRadiusProperty, new CornerRadius(8)),
                new Setter(Button.HorizontalContentAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Center),
                new Setter(Button.VerticalContentAlignmentProperty, Avalonia.Layout.VerticalAlignment.Center),
                new Setter(Button.PaddingProperty, new Thickness(0))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("confirmButton").PropertyEquals(InputElement.IsPointerOverProperty, true))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["ConfirmHoverSurfaceBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["ConfirmHoverBrush"]),
                new Setter(Button.BorderThicknessProperty, new Thickness(1)),
                new Setter(Button.ForegroundProperty, Resources["ConfirmHoverBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("confirmButton").PropertyEquals(Button.IsPressedProperty, true))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["ConfirmPressedSurfaceBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["ConfirmPressedBrush"]),
                new Setter(Button.BorderThicknessProperty, new Thickness(1)),
                new Setter(Button.ForegroundProperty, Resources["ConfirmPressedBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("destructiveButton"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["DangerBrush"]),
                new Setter(Button.BorderBrushProperty, Brush("#72FFFFFF")),
                new Setter(Button.BorderThicknessProperty, new Thickness(1)),
                new Setter(Button.ForegroundProperty, Resources["PrimaryTextBrush"]),
                new Setter(Button.CornerRadiusProperty, new CornerRadius(8)),
                new Setter(Button.HorizontalContentAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Center),
                new Setter(Button.VerticalContentAlignmentProperty, Avalonia.Layout.VerticalAlignment.Center),
                new Setter(Button.PaddingProperty, new Thickness(0))
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("destructiveButton").PropertyEquals(InputElement.IsPointerOverProperty, true))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["DangerHoverBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["PrimaryTextBrush"]),
                new Setter(Button.ForegroundProperty, Resources["PrimaryTextBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("destructiveButton").PropertyEquals(Button.IsPressedProperty, true))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["DangerPressedBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["PrimaryTextBrush"]),
                new Setter(Button.ForegroundProperty, Resources["PrimaryTextBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("iconButton").Class("dangerIconButton").PropertyEquals(InputElement.IsPointerOverProperty, true))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["DangerHoverBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["PrimaryTextBrush"]),
                new Setter(Button.BorderThicknessProperty, new Thickness(1)),
                new Setter(Button.ForegroundProperty, Resources["PrimaryTextBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("iconButton").Class("dangerIconButton").PropertyEquals(Button.IsPressedProperty, true))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["DangerPressedBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["PrimaryTextBrush"]),
                new Setter(Button.BorderThicknessProperty, new Thickness(1)),
                new Setter(Button.ForegroundProperty, Resources["PrimaryTextBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("commentTextButton"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Brushes.Transparent),
                new Setter(Button.BorderBrushProperty, Brushes.Transparent),
                new Setter(Button.BorderThicknessProperty, new Thickness(0)),
                new Setter(Button.ForegroundProperty, Resources["SecondaryTextBrush"]),
                new Setter(Button.CornerRadiusProperty, new CornerRadius(7)),
                new Setter(Button.PaddingProperty, new Thickness(10, 0)),
                new Setter(Button.HeightProperty, 30d),
                new Setter(Button.FontSizeProperty, 12d),
                new Setter(Button.HorizontalContentAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Center),
                new Setter(Button.VerticalContentAlignmentProperty, Avalonia.Layout.VerticalAlignment.Center)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("commentTextButton").PropertyEquals(InputElement.IsPointerOverProperty, true))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["ControlHoverBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["ControlHoverBorderBrush"]),
                new Setter(Button.BorderThicknessProperty, new Thickness(1)),
                new Setter(Button.ForegroundProperty, Resources["PrimaryTextBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("commentTextButton").PropertyEquals(Button.IsPressedProperty, true))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["ControlPressedBrush"]),
                new Setter(Button.BorderBrushProperty, Resources["ControlHoverBorderBrush"]),
                new Setter(Button.BorderThicknessProperty, new Thickness(1)),
                new Setter(Button.ForegroundProperty, Resources["PrimaryTextBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("commentPrimaryButton"))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["ActiveControlBrush"]),
                new Setter(Button.BorderBrushProperty, Brushes.Transparent),
                new Setter(Button.BorderThicknessProperty, new Thickness(0)),
                new Setter(Button.ForegroundProperty, Resources["ActiveControlForegroundBrush"]),
                new Setter(Button.CornerRadiusProperty, new CornerRadius(7)),
                new Setter(Button.PaddingProperty, new Thickness(12, 0)),
                new Setter(Button.HeightProperty, 30d),
                new Setter(Button.FontSizeProperty, 12d),
                new Setter(Button.FontWeightProperty, FontWeight.SemiBold),
                new Setter(Button.HorizontalContentAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Center),
                new Setter(Button.VerticalContentAlignmentProperty, Avalonia.Layout.VerticalAlignment.Center)
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("commentPrimaryButton").PropertyEquals(InputElement.IsPointerOverProperty, true))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["ActiveControlHoverBrush"]),
                new Setter(Button.ForegroundProperty, Resources["ActiveControlForegroundBrush"])
            }
        });
        Styles.Add(new Style(selector => selector.OfType<Button>().Class("commentPrimaryButton").PropertyEquals(Button.IsPressedProperty, true))
        {
            Setters =
            {
                new Setter(Button.BackgroundProperty, Resources["ActiveControlPressedBrush"]),
                new Setter(Button.ForegroundProperty, Resources["ActiveControlForegroundBrush"])
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
                new Setter(TextBox.ForegroundProperty, Resources["PrimaryTextBrush"]),
                new Setter(TextBox.FocusAdornerProperty, null),
                new Setter(TextBox.CaretBrushProperty, Resources["PrimaryTextBrush"]),
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
        Styles.Add(new Style(selector => selector.OfType<TextBox>().Class("commentTextBox").PropertyEquals(InputElement.IsFocusedProperty, true))
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

    internal static string? ReadExportFolder(IReadOnlyList<string> args)
    {
        return ReadOption(args, "--export");
    }

    internal static string? ReadExportRoot(IReadOnlyList<string> args)
    {
        return ReadOption(args, "--export-root");
    }

    internal static int ReadDefaultScale(IReadOnlyList<string> args)
    {
        return CaptureScale.ParseOrDefault(ReadOption(args, "--default-scale"));
    }

    internal static LaunchCaller ReadCaller(IReadOnlyList<string> args)
    {
        return string.Equals(ReadOption(args, "--caller"), "agent", StringComparison.OrdinalIgnoreCase)
            ? LaunchCaller.Agent
            : LaunchCaller.Human;
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

        return DefaultIdleTimeout;
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
