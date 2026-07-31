using AA.Annotate.App.ViewModels;
using AA.Annotate.Platform;
using Avalonia;
using Avalonia.Controls;

namespace AA.Annotate.App.Services;

internal sealed class OverlayWindowCoordinator
{
    private readonly Window _overlay;
    private readonly ToolbarWindow _toolbar;
    private readonly IWindowIntegration _windowIntegration;
    private readonly Action _clampToolbar;
    private bool _toolbarWasShown;

    public OverlayWindowCoordinator(
        Window overlay,
        ToolbarWindow toolbar,
        IWindowIntegration windowIntegration,
        Action clampToolbar)
    {
        _overlay = overlay;
        _toolbar = toolbar;
        _windowIntegration = windowIntegration;
        _clampToolbar = clampToolbar;
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
    }

    public void RevealToolbar()
    {
        _clampToolbar();
        _toolbar.Opacity = 1;
        RaiseToolbarWithoutActivation();
    }

    public void Apply(OverlayPresentation presentation, DisplayDescriptor? display)
    {
        _toolbar.IsEnabled = presentation.ToolbarEnabled;

        if (!presentation.ToolbarVisible)
        {
            _toolbar.ClosePopups();
            if (_toolbar.IsVisible)
            {
                _toolbar.Hide();
            }
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
        }
        else if (_overlay.IsVisible)
        {
            _overlay.Hide();
        }

        if (presentation.ToolbarVisible && !_toolbar.IsVisible)
        {
            _toolbar.Show();
            SuppressBorder(_toolbar);
            _clampToolbar();
        }

        if (presentation.ToolbarVisible)
        {
            RaiseToolbarWithoutActivation();
        }
    }

    public void CloseToolbarPopups()
    {
        _toolbar.ClosePopups();
    }

    public void CloseToolbar()
    {
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

        _overlay.Position = bounds.Position;
        _overlay.Width = bounds.Width / scaling;
        _overlay.Height = bounds.Height / scaling;
    }

    private void SuppressBorder(Window window)
    {
        if (window.TryGetPlatformHandle()?.Handle is { } handle)
        {
            _windowIntegration.SuppressBorder(handle);
        }
    }

    private void RaiseToolbarWithoutActivation()
    {
        if (_toolbar.TryGetPlatformHandle()?.Handle is { } handle)
        {
            _windowIntegration.BringToFrontWithoutActivation(handle);
        }
    }
}
