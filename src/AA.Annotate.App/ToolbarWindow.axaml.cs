using AA.Annotate.App.Views;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace AA.Annotate.App;

public partial class ToolbarWindow : Window
{
    private readonly HashSet<Control> _hoveredPanels = [];

    internal event EventHandler? DragStarted;

    internal event EventHandler? DragCompleted;

    public ToolbarWindow()
    {
        InitializeComponent();

        ConfigurePanelHover(CommandBarElement);
        ConfigurePanelHover(CaptureScaleSelectorElement);
        ConfigurePanelHover(DisplayDropdownElement);
        ConfigurePanelHover(CaptureDropdownElement);
        ConfigurePanelHover(AboutPanelElement);
        ConfigurePanelHover(CaptureStatusPanelElement);

        CommandBarElement.DragRequested += OnDragRequested;
        AboutCloseButtonElement.Click += (_, _) => IsAboutPanelOpen = false;
        CaptureStatusDismissButtonElement.Click += (_, _) => IsCaptureStatusOpen = false;
    }

    internal FloatingCommandBar CommandBarView => CommandBarElement;

    internal CaptureScaleSelector CaptureScaleSelectorView => CaptureScaleSelectorElement;

    internal DisplayDropdown DisplayDropdownView => DisplayDropdownElement;

    internal CaptureDropdown CaptureDropdownView => CaptureDropdownElement;

    internal Border AboutPanelView => AboutPanelElement;

    internal Button AboutCloseButton => AboutCloseButtonElement;

    internal TextBlock AboutVersionText => AboutVersionTextElement;

    internal TextBlock AboutGitHubLinkText => AboutGitHubLinkTextElement;

    internal Border CaptureStatusPanelView => CaptureStatusPanelElement;

    internal Button CaptureStatusDismissButton => CaptureStatusDismissButtonElement;

    internal TextBlock CaptureStatusTitleText => CaptureStatusTitleTextElement;

    internal TextBlock CaptureStatusMessageText => CaptureStatusMessageTextElement;

    internal bool IsDisplayDropdownOpen
    {
        get => DisplayDropdownPopup.IsOpen;
        set
        {
            DisplayDropdownPopup.IsOpen = value;
            ClearClosedPopupHover(DisplayDropdownElement, value);
        }
    }

    internal bool IsCaptureDropdownOpen
    {
        get => CaptureDropdownPopup.IsOpen;
        set
        {
            CaptureDropdownPopup.IsOpen = value;
            ClearClosedPopupHover(CaptureDropdownElement, value);
        }
    }

    internal bool IsAboutPanelOpen
    {
        get => AboutPanelPopup.IsOpen;
        set
        {
            AboutPanelPopup.IsOpen = value;
            ClearClosedPopupHover(AboutPanelElement, value);
        }
    }

    internal bool IsCaptureStatusOpen
    {
        get => CaptureStatusPopup.IsOpen;
        set
        {
            CaptureStatusPopup.IsOpen = value;
            ClearClosedPopupHover(CaptureStatusPanelElement, value);
        }
    }

    internal void ClosePopups()
    {
        IsDisplayDropdownOpen = false;
        IsCaptureDropdownOpen = false;
        IsAboutPanelOpen = false;
        IsCaptureStatusOpen = false;
    }

    private void OnDragRequested(object? sender, PointerPressedEventArgs e)
    {
        ClosePopups();
        DragStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            BeginMoveDrag(e);
        }
        finally
        {
            DragCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ConfigurePanelHover(Control panel)
    {
        panel.PointerEntered += (_, _) =>
        {
            _hoveredPanels.Add(panel);
            UpdatePanelHoverState();
        };
        panel.PointerExited += (_, _) =>
        {
            _hoveredPanels.Remove(panel);
            UpdatePanelHoverState();
        };
    }

    private void ClearClosedPopupHover(Control panel, bool isOpen)
    {
        if (!isOpen && _hoveredPanels.Remove(panel))
        {
            UpdatePanelHoverState();
        }
    }

    private void UpdatePanelHoverState()
    {
        var isActive = _hoveredPanels.Any(panel => panel.IsVisible);
        CommandBarElement.SetPanelHoverActive(isActive);
        DisplayDropdownElement.SetPanelHoverActive(isActive);
        CaptureDropdownElement.SetPanelHoverActive(isActive);
        SetAboutPanelHoverActive();
    }

    private void SetAboutPanelHoverActive()
    {
        AboutPanelElement.Opacity = 1;
        AboutPanelElement.Background = App.Current?.FindResource("PanelSurfaceBrush") as IBrush;
    }
}
