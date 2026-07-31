using AA.Annotate.App.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;

namespace AA.Annotate.App.Services;

internal sealed class ToolbarPlacementController : IDisposable
{
    private static readonly TimeSpan SaveDelay = TimeSpan.FromMilliseconds(400);
    private readonly ToolbarWindow _window;
    private readonly UiSettingsStore _store;
    private readonly DispatcherTimer _saveTimer;
    private readonly SemaphoreSlim _saveGate = new(1, 1);
    private Size _lastSize;
    private bool _isApplyingPlacement;
    private bool _isInitialized;
    private bool _isDisposed;

    public ToolbarPlacementController(ToolbarWindow window, UiSettingsStore store)
    {
        _window = window;
        _store = store;
        _saveTimer = new DispatcherTimer { Interval = SaveDelay };
        _saveTimer.Tick += OnSaveTimerTick;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized || _isDisposed)
        {
            return;
        }

        _isInitialized = true;
        _window.UpdateLayout();
        _lastSize = GetToolbarSize();
        var settings = await _store.LoadAsync();
        if (_isDisposed)
        {
            return;
        }

        ApplyPosition(ToolbarPlacementProjector.Restore(
            settings.Toolbar,
            _lastSize,
            GetDisplays()));

        _window.PositionChanged += OnPositionChanged;
        _window.LayoutUpdated += OnLayoutUpdated;
        _window.Screens.Changed += OnScreensChanged;
    }

    public async Task FlushAsync()
    {
        if (!_isInitialized || _isDisposed)
        {
            return;
        }

        _saveTimer.Stop();
        await SaveAsync();
    }

    public void ClampToVisibleArea()
    {
        if (!_isInitialized || _isDisposed)
        {
            return;
        }

        var displays = GetDisplays();
        var size = GetToolbarSize();
        var display = ToolbarPlacementProjector.FindDisplay(
                _window.Position,
                size,
                displays,
                GetToolbarScaling())
            ?? displays.First();
        ApplyPosition(ToolbarPlacementProjector.Clamp(_window.Position, size, display));
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _saveTimer.Stop();
        _saveTimer.Tick -= OnSaveTimerTick;
        if (_isInitialized)
        {
            _window.PositionChanged -= OnPositionChanged;
            _window.LayoutUpdated -= OnLayoutUpdated;
            _window.Screens.Changed -= OnScreensChanged;
        }

    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (_isApplyingPlacement)
        {
            return;
        }

        ScheduleSave();
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        var currentSize = GetToolbarSize();
        if (NearlyEqual(currentSize, _lastSize))
        {
            return;
        }

        _lastSize = currentSize;
        ClampToVisibleArea();
        ScheduleSave();
    }

    private void OnScreensChanged(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ClampToVisibleArea();
            ScheduleSave();
        }, DispatcherPriority.Loaded);
    }

    private async void OnSaveTimerTick(object? sender, EventArgs e)
    {
        _saveTimer.Stop();
        await SaveAsync();
    }

    private void ScheduleSave()
    {
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private async Task SaveAsync()
    {
        var displays = GetDisplays();
        var placement = ToolbarPlacementProjector.Project(
            _window.Position,
            GetToolbarSize(),
            displays,
            toolbarScaling: GetToolbarScaling());

        await _saveGate.WaitAsync();
        try
        {
            await _store.SaveAsync(new UiSettings { Toolbar = placement });
        }
        finally
        {
            _saveGate.Release();
        }
    }

    private void ApplyPosition(PixelPoint position)
    {
        _isApplyingPlacement = true;
        try
        {
            _window.Position = position;
        }
        finally
        {
            _isApplyingPlacement = false;
        }
    }

    private Size GetToolbarSize()
    {
        var size = _window.ClientSize;
        if (size.Width > 0 && size.Height > 0)
        {
            return size;
        }

        if (_window.Content is Layoutable content)
        {
            content.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return content.DesiredSize;
        }

        return new Size(1, 1);
    }

    private double GetToolbarScaling()
    {
        return double.IsFinite(_window.RenderScaling) && _window.RenderScaling > 0
            ? _window.RenderScaling
            : 1;
    }

    private IReadOnlyList<ToolbarDisplay> GetDisplays()
    {
        var displays = _window.Screens.All
            .Select(screen => new ToolbarDisplay(
                CreateDisplayName(screen),
                screen.Bounds,
                screen.WorkingArea,
                screen.Scaling,
                screen.IsPrimary))
            .ToList();

        if (displays.Count == 0)
        {
            throw new InvalidOperationException("No displays are available.");
        }

        return displays;
    }

    private static string CreateDisplayName(Avalonia.Platform.Screen screen)
    {
        return string.IsNullOrWhiteSpace(screen.DisplayName)
            ? $"{screen.Bounds.X},{screen.Bounds.Y},{screen.Bounds.Width},{screen.Bounds.Height}"
            : screen.DisplayName;
    }

    private static bool NearlyEqual(Size first, Size second)
    {
        return Math.Abs(first.Width - second.Width) < 0.5
            && Math.Abs(first.Height - second.Height) < 0.5;
    }
}
