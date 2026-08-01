using AA.Annotate.App.Views;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;

namespace AA.Annotate.App;

public partial class ToolbarWindow : Window
{
    internal event EventHandler? DragStarted;

    internal event EventHandler? DragCompleted;

    public ToolbarWindow()
    {
        InitializeComponent();
        if (OperatingSystem.IsLinux())
        {
            // KWin can consume normal clicks on a non-activating toolbar as
            // focus-only input. The Linux toolbar is an interactive command
            // surface, so let it activate when it is shown or clicked.
            ShowActivated = true;
        }

        // KWin treats an initially non-activating TOOLBAR window as passive
        // chrome and can consume ordinary button clicks. This is still styled
        // as a borderless, skip-taskbar toolbar, but advertises an interactive
        // NORMAL X11 type so KDE gives it standard input semantics.
        X11Properties.SetNetWmWindowType(this, X11NetWmWindowType.Normal);
        Surface = new ToolbarSurface();
        AttachSurface();
        Surface.DragRequested += OnDragRequested;
    }

    internal ToolbarSurface Surface { get; }

    internal FloatingCommandBar CommandBarView => Surface.CommandBarView;

    internal CaptureScaleSelector CaptureScaleSelectorView => Surface.CaptureScaleSelectorView;

    internal DisplayDropdown DisplayDropdownView => Surface.DisplayDropdownView;

    internal CaptureDropdown CaptureDropdownView => Surface.CaptureDropdownView;

    internal Border AboutPanelView => Surface.AboutPanelView;

    internal Button AboutCloseButton => Surface.AboutCloseButton;

    internal TextBlock AboutVersionText => Surface.AboutVersionText;

    internal TextBlock AboutGitHubLinkText => Surface.AboutGitHubLinkText;

    internal Border CaptureStatusPanelView => Surface.CaptureStatusPanelView;

    internal Button CaptureStatusDismissButton => Surface.CaptureStatusDismissButton;

    internal TextBlock CaptureStatusTitleText => Surface.CaptureStatusTitleText;

    internal TextBlock CaptureStatusMessageText => Surface.CaptureStatusMessageText;

    internal bool IsDisplayDropdownOpen
    {
        get => Surface.IsDisplayDropdownOpen;
        set => Surface.IsDisplayDropdownOpen = value;
    }

    internal bool IsCaptureDropdownOpen
    {
        get => Surface.IsCaptureDropdownOpen;
        set => Surface.IsCaptureDropdownOpen = value;
    }

    internal bool IsAboutPanelOpen
    {
        get => Surface.IsAboutPanelOpen;
        set => Surface.IsAboutPanelOpen = value;
    }

    internal bool IsCaptureStatusOpen
    {
        get => Surface.IsCaptureStatusOpen;
        set => Surface.IsCaptureStatusOpen = value;
    }

    internal bool IsSurfaceAttached => ReferenceEquals(ToolbarHost.Content, Surface);

    internal void AttachSurface()
    {
        ToolbarHost.Content = Surface;
    }

    internal void DetachSurface()
    {
        if (IsSurfaceAttached)
        {
            ToolbarHost.Content = null;
        }
    }

    internal void ClosePopups()
    {
        Surface.ClosePopups();
    }

    private void OnDragRequested(object? sender, PointerPressedEventArgs e)
    {
        if (!IsSurfaceAttached)
        {
            return;
        }

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
}
