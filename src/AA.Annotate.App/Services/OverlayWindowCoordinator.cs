using AA.Annotate.App.ViewModels;
using AA.Annotate.App.Views;
using AA.Annotate.Platform;
using Avalonia;
using Avalonia.Controls;

namespace AA.Annotate.App.Services;

internal sealed class OverlayWindowCoordinator
{
    private static readonly TimeSpan LinuxTopmostPulseInterval = TimeSpan.FromMilliseconds(250);
    private readonly Window _overlay;
    private readonly ToolbarWindow _toolbar;
    private readonly ContentControl _embeddedToolbarHost;
    private readonly IWindowIntegration _windowIntegration;
    private readonly Action _clampToolbar;
    private readonly Action _positionEmbeddedToolbar;
    private readonly TopmostHeartbeat? _topmostHeartbeat;
    private bool _toolbarWasShown;
    private bool _usingEmbeddedToolbar;
    private PixelRect? _fullscreenBounds;

    public OverlayWindowCoordinator(
        Window overlay,
        ToolbarWindow toolbar,
        ContentControl embeddedToolbarHost,
        IWindowIntegration windowIntegration,
        Action clampToolbar,
        Action positionEmbeddedToolbar)
    {
        _overlay = overlay;
        _toolbar = toolbar;
        _embeddedToolbarHost = embeddedToolbarHost;
        _windowIntegration = windowIntegration;
        _clampToolbar = clampToolbar;
        _positionEmbeddedToolbar = positionEmbeddedToolbar;
        if (OperatingSystem.IsLinux())
        {
            _topmostHeartbeat = new TopmostHeartbeat(
                LinuxTopmostPulseInterval,
                KeepToolbarAboveOverlay);
        }
    }

    public void InitializeToolbar()
    {
        if (_toolbarWasShown)
        {
            return;
        }

        _toolbar.Opacity = 0;
        // The passive overlay is intentionally hidden. On Windows, hiding an
        // owner also hides its owned windows, so the idle toolbar must be an
        // independently coordinated top-level window.
        _toolbar.Show();
        _toolbarWasShown = true;
        SuppressBorder(_toolbar);
        SetAlwaysOnTop(_toolbar, enabled: true);
    }

    public void RevealToolbar()
    {
        _clampToolbar();
        _toolbar.Opacity = 1;
        SetAlwaysOnTop(_toolbar, enabled: true);
        if (OperatingSystem.IsLinux())
        {
            _toolbar.Activate();
        }

        _topmostHeartbeat?.SetEnabled(true);
        KeepToolbarAboveOverlay();
    }

    public bool IsUsingEmbeddedToolbar => _usingEmbeddedToolbar;

    public void KeepToolbarAboveOverlay()
    {
        if (_usingEmbeddedToolbar)
        {
            RaiseOverlayWithoutActivation();
            return;
        }

        if (_toolbarWasShown && _toolbar.IsVisible && _toolbar.Opacity > 0)
        {
            RaiseToolbarWithoutActivation();
        }
    }

    public void Apply(OverlayPresentation presentation, DisplayDescriptor? display)
    {
        _toolbar.IsEnabled = presentation.ToolbarEnabled;
        _toolbar.Surface.IsEnabled = presentation.ToolbarEnabled;
        if (_embeddedToolbarHost.Content is Control embeddedToolbar)
        {
            embeddedToolbar.IsEnabled = presentation.ToolbarEnabled;
        }

        if (!presentation.ToolbarVisible)
        {
            _toolbar.ClosePopups();
            if (_toolbar.IsVisible)
            {
                _toolbar.Hide();
            }

            EmbeddedToolbarLayerVisible = false;
        }

        if (presentation.OverlayVisible)
        {
            if (display is not null)
            {
                PlaceOverlay(display);
            }

            if (!_overlay.IsVisible)
            {
                _overlay.Show();
            }

            SuppressBorder(_overlay);
            SetAlwaysOnTop(_overlay, enabled: true);
        }
        else if (_overlay.IsVisible)
        {
            SetAlwaysOnTop(_overlay, enabled: false);
            _overlay.Hide();
            RestoreOverlayWindowState();
        }

        _usingEmbeddedToolbar = OperatingSystem.IsLinux() && presentation.OverlayVisible;
        if (_usingEmbeddedToolbar)
        {
            _toolbar.ClosePopups();
            if (_toolbar.IsVisible)
            {
                SetAlwaysOnTop(_toolbar, enabled: false);
                _toolbar.Hide();
            }

            EmbeddedToolbarLayerVisible = presentation.ToolbarVisible;
            if (presentation.ToolbarVisible)
            {
                _positionEmbeddedToolbar();
            }
        }
        else
        {
            if (_embeddedToolbarHost.Content is ToolbarSurface embeddedSurface)
            {
                embeddedSurface.ClosePopups();
            }

            EmbeddedToolbarLayerVisible = false;
            if (presentation.ToolbarVisible && !_toolbar.IsVisible)
            {
                _toolbar.Show();
                SuppressBorder(_toolbar);
                SetAlwaysOnTop(_toolbar, enabled: true);
                _clampToolbar();
            }
        }

        if (presentation.ToolbarVisible)
        {
            KeepToolbarAboveOverlay();
        }

        _topmostHeartbeat?.SetEnabled(
            presentation.OverlayVisible || presentation.ToolbarVisible);
    }

    public void CloseToolbarPopups()
    {
        _toolbar.ClosePopups();
        if (_embeddedToolbarHost.Content is ToolbarSurface embeddedSurface)
        {
            embeddedSurface.ClosePopups();
        }
    }

    public void CloseToolbar()
    {
        _topmostHeartbeat?.Dispose();
        _toolbar.Close();
    }

    private void PlaceOverlay(DisplayDescriptor display)
    {
        var screen = _overlay.Screens.ScreenFromPoint(new PixelPoint(
            display.Bounds.X + display.Bounds.Width / 2,
            display.Bounds.Y + display.Bounds.Height / 2));
        var bounds = screen?.Bounds
            ?? new PixelRect(
                display.Bounds.X,
                display.Bounds.Y,
                display.Bounds.Width,
                display.Bounds.Height);
        var scaling = screen?.Scaling > 0
            ? screen.Scaling
            : (_overlay.RenderScaling > 0 ? _overlay.RenderScaling : 1);

        if (OperatingSystem.IsLinux() &&
            _fullscreenBounds == bounds &&
            _overlay.WindowState == WindowState.FullScreen)
        {
            return;
        }

        if (OperatingSystem.IsLinux() && _overlay.WindowState == WindowState.FullScreen)
        {
            _overlay.WindowState = WindowState.Normal;
        }

        _overlay.Position = bounds.Position;
        _overlay.Width = bounds.Width / scaling;
        _overlay.Height = bounds.Height / scaling;

        if (OperatingSystem.IsLinux())
        {
            _fullscreenBounds = bounds;
            _overlay.WindowState = WindowState.FullScreen;
        }
    }

    private void RestoreOverlayWindowState()
    {
        if (OperatingSystem.IsLinux() && _overlay.WindowState == WindowState.FullScreen)
        {
            _overlay.WindowState = WindowState.Normal;
        }
    }

    private bool EmbeddedToolbarLayerVisible
    {
        get => _embeddedToolbarHost.IsVisible;
        set
        {
            _embeddedToolbarHost.IsVisible = value;
            if (_embeddedToolbarHost.Parent is Control layer)
            {
                layer.IsVisible = value;
            }
        }
    }

    private void SuppressBorder(Window window)
    {
        if (TryGetSupportedNativeHandle(window, out var handle))
        {
            _windowIntegration.SuppressBorder(handle);
        }
    }

    private void SetAlwaysOnTop(Window window, bool enabled)
    {
        if (TryGetSupportedNativeHandle(window, out var handle))
        {
            _windowIntegration.SetAlwaysOnTop(handle, enabled);
        }
    }

    private void RaiseToolbarWithoutActivation()
    {
        if (TryGetSupportedNativeHandle(_toolbar, out var handle))
        {
            _windowIntegration.BringToFrontWithoutActivation(handle);
        }
    }

    private void RaiseOverlayWithoutActivation()
    {
        if (TryGetSupportedNativeHandle(_overlay, out var handle))
        {
            _windowIntegration.BringToFrontWithoutActivation(handle);
        }
    }

    private static bool TryGetSupportedNativeHandle(Window window, out nint handle)
    {
        var platformHandle = window.TryGetPlatformHandle();
        handle = platformHandle?.Handle ?? 0;
        if (handle == 0)
        {
            return false;
        }

        if (!OperatingSystem.IsLinux())
        {
            return true;
        }

        return string.Equals(platformHandle?.HandleDescriptor, "X11", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(platformHandle?.HandleDescriptor, "XID", StringComparison.OrdinalIgnoreCase);
    }
}
