using System.Drawing.Imaging;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using AA.Annotate.App.Services;
using AA.Annotate.App.ViewModels;
using AA.Annotate.App.Views;
using AA.Annotate.Core.Geometry;
using AA.Annotate.Core.Models;
using AA.Annotate.Core.Serialization;
using AA.Annotate.Core.Services;
using AA.Annotate.Platform;
using AA.Annotate.Platform.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using DrawingBitmap = System.Drawing.Bitmap;
using DrawingGraphics = System.Drawing.Graphics;
using DrawingGraphicsUnit = System.Drawing.GraphicsUnit;
using DrawingRectangle = System.Drawing.Rectangle;

namespace AA.Annotate.App;

public partial class MainWindow : Window
{
    private const int MinimumAnnotationSize = AnnotationRectPolicy.MinimumSize;
    private const string GitHubRepositoryUrl = "https://github.com/adiladiloglu/AA.Annotate";
    private static readonly TimeSpan ActivityWriteInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan IdleWarningDuration = TimeSpan.FromSeconds(30);
    private readonly AnnotationSessionViewModel _session = new();
    private readonly SessionStore _store = new();
    private readonly SessionExporter _exporter = new(new AnnotationArtifactWriter());
    private readonly IDisplayCatalog _displayCatalog = new WindowsDisplayCatalog();
    private readonly IScreenCaptureService _captureService = new WindowsScreenCaptureService();
    private readonly HashSet<Control> _hoveredChromePanels = [];
    private readonly TimeSpan? _idleTimeout;
    private readonly string? _providedSessionFolder;
    private readonly string? _providedSessionRoot;
    private SessionPaths? _paths;
    private SessionStatusDocument? _status;
    private bool _isCapturing;
    private bool _isDrawing;
    private bool _hasTerminalStatus;
    private bool _isAnnotationToggleActive;
    private DisplayDescriptor? _activeDisplay;
    private Point _drawStart;
    private Border? _draftBox;
    private Border? _draftWarning;
    private AnnotationViewModel? _commentTarget;
    private DateTimeOffset _lastActivityWriteUtc = DateTimeOffset.MinValue;
    private DateTimeOffset _lastUserActivityUtc = DateTimeOffset.UtcNow;
    private DateTimeOffset _idleWarningExpiresAtUtc;
    private DispatcherTimer? _idleTimer;
    private DispatcherTimer? _idleWarningTimer;

    public MainWindow()
        : this(null, null, null)
    {
    }

    public MainWindow(string? sessionFolder)
        : this(sessionFolder, null, null)
    {
    }

    public MainWindow(string? sessionFolder, TimeSpan? idleTimeout)
        : this(sessionFolder, null, idleTimeout)
    {
    }

    public MainWindow(string? sessionFolder, string? sessionRoot, TimeSpan? idleTimeout)
    {
        _providedSessionFolder = sessionFolder;
        _providedSessionRoot = sessionRoot;
        _idleTimeout = idleTimeout;
        InitializeComponent();
        ConfigureChromePanelHover(CommandBar);
        ConfigureChromePanelHover(DisplayDropdown);
        ConfigureChromePanelHover(CaptureDropdown);
        ConfigureChromePanelHover(AboutPanel);
        Opened += OnOpened;
        Closing += OnClosing;
        AnnotationCanvas.PointerPressed += OnAnnotationPointerPressed;
        AnnotationCanvas.PointerMoved += OnAnnotationPointerMoved;
        AnnotationCanvas.PointerReleased += OnAnnotationPointerReleased;
        CommandBar.MoveSelectorRequested += (_, _) => ToggleDisplayDropdown();
        CommandBar.CaptureRequested += async (_, _) => await RequestCaptureAsync();
        CommandBar.CaptureSelectorRequested += (_, _) => ToggleCaptureDropdown();
        CommandBar.CropRequested += async (_, _) => await ActivateCropAsync();
        CommandBar.AnnotationRequested += async (_, _) => await ActivateAnnotationAsync();
        CommandBar.FinishRequested += async (_, _) => await FinishAsync();
        CommandBar.AboutRequested += (_, _) => ToggleAboutPanel();
        CommandBar.CancelRequested += async (_, _) => await CancelAsync();
        DisplayDropdown.DisplaySelected += (_, display) => MoveToDisplay(display.Display);
        CaptureDropdown.CaptureSelected += (_, capture) => SelectCaptureForAnnotation(capture);
        CaptureDropdown.CaptureDeleteRequested += (_, capture) => DeleteCapture(capture);
        CaptureDropdown.NewCaptureRequested += async (_, _) => await RequestCaptureAsync();
        CommentEditor.DeleteRequested += (_, _) => DeleteCommentTarget();
        CommentEditor.CancelRequested += (_, _) => CancelCommentTarget();
        CommentEditor.SaveRequested += (_, text) => SaveCommentTarget(text);
        CropOverlay.CropChanged += (_, crop) =>
        {
            BlurredCropMask.SetCrop(crop);
            if (_session.CurrentCapture is { } capture)
            {
                RememberCurrentViewport(capture);
                CaptureCropProjector.CommitViewportCrop(capture, crop);
                UpdateAnnotationExportStates(capture);
            }
        };
        IdleWarningContinueButton.Click += (_, _) => ContinueAfterIdleWarning();
        IdleWarningDiscardButton.Click += async (_, _) => await CancelAsync();
        IdleWarningSendButton.Click += async (_, _) => await FinishAsync();
        AboutCloseButton.Click += (_, _) => CloseAboutPanel();
        AboutGitHubLinkText.PointerPressed += (_, _) => OpenGitHubRepository();
        AddHandler(PointerPressedEvent, OnUserActivity, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerMovedEvent, OnUserActivity, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnUserActivity, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerWheelChangedEvent, OnUserActivity, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(KeyDownEvent, OnUserActivity, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        SuppressNativeWindowBorder();
        PlaceOnPrimaryDisplay();
        await EnsureSessionAsync();
        AboutVersionText.Text = CreateAboutVersionText();
        ResetIdleTimer();
        UpdateChrome();
        CommandBar.PlayStartupAttentionAnimation();
    }

    private void SuppressNativeWindowBorder()
    {
        if (TryGetPlatformHandle()?.Handle is { } handle)
        {
            WindowsNativeWindowChrome.SuppressBorder(handle);
        }
    }

    private void ConfigureChromePanelHover(Control panel)
    {
        panel.PointerEntered += (_, _) =>
        {
            _hoveredChromePanels.Add(panel);
            UpdateChromePanelHoverState();
        };
        panel.PointerExited += (_, _) =>
        {
            _hoveredChromePanels.Remove(panel);
            UpdateChromePanelHoverState();
        };
    }

    private void UpdateChromePanelHoverState()
    {
        var isActive = _hoveredChromePanels.Any(panel => panel.IsVisible);
        CommandBar.SetPanelHoverActive(isActive);
        DisplayDropdown.SetPanelHoverActive(isActive);
        CaptureDropdown.SetPanelHoverActive(isActive);
        SetAboutPanelHoverActive(isActive);
    }

    private void SetAboutPanelHoverActive(bool isActive)
    {
        var panelBrush = App.Current?.FindResource("PanelSurfaceBrush") as IBrush;
        AboutPanel.Opacity = 1;
        AboutPanel.Background = panelBrush;
    }

    private static void OpenGitHubRepository()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = GitHubRepositoryUrl,
                UseShellExecute = true
            });
        }
        catch
        {
            // The link is informational if the shell cannot open a browser.
        }
    }

    private async Task EnsureSessionAsync()
    {
        if (string.IsNullOrWhiteSpace(_providedSessionFolder))
        {
            _paths = await _store.CreateSessionAsync(_providedSessionRoot);
            _status = await _store.ReadStatusAsync(_paths);
            return;
        }

        Directory.CreateDirectory(_providedSessionFolder);
        Directory.CreateDirectory(Path.Combine(_providedSessionFolder, "captures"));
        _paths = SessionPaths.FromFolder(_providedSessionFolder);
        _status = File.Exists(_paths.StatusJsonPath)
            ? await _store.ReadStatusAsync(_paths)
            : await InitializeProvidedSessionAsync(_paths);
    }

    private static async Task<SessionStatusDocument> InitializeProvidedSessionAsync(SessionPaths paths)
    {
        var now = DateTimeOffset.UtcNow;
        var status = new SessionStatusDocument(
            SessionStatus.Waiting,
            Path.GetFileName(paths.SessionFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
            now,
            CompletedAtUtc: null,
            CancelledAtUtc: null,
            ReviewPath: null,
            AnnotationsPath: null,
            ErrorMessage: null)
        {
            LastActivityAtUtc = now
        };
        await using var stream = File.Create(paths.StatusJsonPath);
        await JsonSerializer.SerializeAsync(stream, status, SessionJsonOptions.Create());
        return status;
    }

    private void OnUserActivity(object? sender, RoutedEventArgs e)
    {
        if (!IdleWarningOverlay.IsVisible)
        {
            ResetIdleTimer();
        }

        _ = TouchActivityAsync();
    }

    private async Task TouchActivityAsync()
    {
        if (_paths is null || _hasTerminalStatus)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        if (now - _lastActivityWriteUtc < ActivityWriteInterval)
        {
            return;
        }

        _lastActivityWriteUtc = now;
        try
        {
            await _store.TouchActivityAsync(_paths);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
        }
    }

    private void ResetIdleTimer()
    {
        if (_idleTimeout is null || _hasTerminalStatus)
        {
            return;
        }

        _lastUserActivityUtc = DateTimeOffset.UtcNow;
        _idleTimer ??= new DispatcherTimer { Interval = _idleTimeout.Value };
        _idleTimer.Stop();
        _idleTimer.Interval = _idleTimeout.Value;
        _idleTimer.Tick -= OnIdleTimerTick;
        _idleTimer.Tick += OnIdleTimerTick;
        _idleTimer.Start();
    }

    private void OnIdleTimerTick(object? sender, EventArgs e)
    {
        _idleTimer?.Stop();
        if (_hasTerminalStatus || IdleWarningOverlay.IsVisible)
        {
            return;
        }

        var idleFor = DateTimeOffset.UtcNow - _lastUserActivityUtc;
        if (_idleTimeout is { } timeout && idleFor < timeout)
        {
            ResetIdleTimer();
            return;
        }

        ShowIdleWarning();
    }

    private void ShowIdleWarning()
    {
        IdleWarningOverlay.IsVisible = true;
        IdleWarningSendButton.IsVisible = HasAnyAnnotations();
        IdleWarningMessageText.Text = HasAnyAnnotations()
            ? "Send the current annotations now, discard them, or continue working."
            : "Continue working or discard this inactive session.";
        if (_activeDisplay is { } display)
        {
            SetActiveDisplay(display, fullscreen: true);
        }

        _idleWarningExpiresAtUtc = DateTimeOffset.UtcNow + IdleWarningDuration;
        _idleWarningTimer ??= new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _idleWarningTimer.Stop();
        _idleWarningTimer.Tick -= OnIdleWarningTimerTick;
        _idleWarningTimer.Tick += OnIdleWarningTimerTick;
        _idleWarningTimer.Start();
        UpdateIdleWarningCountdown();
    }

    private void ContinueAfterIdleWarning()
    {
        _idleWarningTimer?.Stop();
        IdleWarningOverlay.IsVisible = false;
        ResetIdleTimer();
        ApplyCurrentWindowMode();
        _ = TouchActivityAsync();
    }

    private async void OnIdleWarningTimerTick(object? sender, EventArgs e)
    {
        if (DateTimeOffset.UtcNow < _idleWarningExpiresAtUtc)
        {
            UpdateIdleWarningCountdown();
            return;
        }

        _idleWarningTimer?.Stop();
        await CancelAsync();
    }

    private void UpdateIdleWarningCountdown()
    {
        var remaining = Math.Max(0, (int)Math.Ceiling((_idleWarningExpiresAtUtc - DateTimeOffset.UtcNow).TotalSeconds));
        IdleWarningCountdownText.Text = $"Closing without annotations in {remaining} seconds.";
    }

    private bool HasAnyAnnotations()
    {
        return _session.Captures.Any(capture => capture.Annotations.Count > 0);
    }

    private void PlaceOnPrimaryDisplay()
    {
        var display = _displayCatalog.GetDisplays().FirstOrDefault(screen => screen.IsPrimary)
            ?? _displayCatalog.GetDisplays().First();
        SetActiveDisplay(display, fullscreen: false);
    }

    private void PlaceOnDisplay(DisplayDescriptor display, bool fullscreen)
    {
        _activeDisplay = display;
        var screen = Screens.ScreenFromPoint(new PixelPoint(
            display.Bounds.X + display.Bounds.Width / 2,
            display.Bounds.Y + display.Bounds.Height / 2));
        var bounds = screen?.Bounds ?? new PixelRect(display.Bounds.X, display.Bounds.Y, display.Bounds.Width, display.Bounds.Height);
        var scaling = screen?.Scaling > 0 ? screen.Scaling : RenderScaling;

        if (fullscreen)
        {
            ChromeCanvas.RenderTransform = null;
            Position = new PixelPoint(bounds.X, bounds.Y);
            Width = bounds.Width / scaling;
            Height = bounds.Height / scaling;
            return;
        }

        var footprint = MeasureVisibleChromeFootprint();
        ChromeCanvas.RenderTransform = new TranslateTransform(-footprint.X, -footprint.Y);
        Position = new PixelPoint(
            bounds.X + (int)Math.Round(footprint.X * scaling),
            bounds.Y + (int)Math.Round(footprint.Y * scaling));
        Width = footprint.Width;
        Height = footprint.Height;
    }

    private void ToggleDisplayDropdown()
    {
        if (_isCapturing)
        {
            return;
        }

        DisplayDropdown.IsVisible = !DisplayDropdown.IsVisible;
        CaptureDropdown.IsVisible = false;
        AboutPanel.IsVisible = false;
        ApplyCurrentWindowMode();
        UpdateChrome();
    }

    private void ToggleAboutPanel()
    {
        if (_isCapturing)
        {
            return;
        }

        AboutPanel.IsVisible = !AboutPanel.IsVisible;
        DisplayDropdown.IsVisible = false;
        CaptureDropdown.IsVisible = false;
        CommentEditor.IsVisible = false;
        ApplyCurrentWindowMode();
        UpdateChrome();
    }

    private void CloseAboutPanel()
    {
        AboutPanel.IsVisible = false;
        ApplyCurrentWindowMode();
        UpdateChrome();
    }

    private void MoveToDisplay(DisplayDescriptor display)
    {
        if (_isCapturing)
        {
            return;
        }

        StoreCurrentCrop();
        DisplayDropdown.IsVisible = false;
        AboutPanel.IsVisible = false;
        CommentEditor.IsVisible = false;
        CropOverlay.IsVisible = false;
        SetActiveDisplay(display, IsBlockingSurfaceActive());
        RefreshCropMaskVisibility();
        UpdateChrome();
    }

    private void SetActiveDisplay(DisplayDescriptor display, bool fullscreen)
    {
        PlaceOnDisplay(display, fullscreen);
        SuppressNativeWindowBorder();
    }

    private async Task CaptureAsync(bool activateAnnotationAfterCapture = true)
    {
        if (_paths is null || _isCapturing)
        {
            return;
        }

        StoreCurrentCrop();
        var previousCapture = _session.CurrentCapture;
        _isCapturing = true;
        DisplayDropdown.IsVisible = false;
        CaptureDropdown.IsVisible = false;
        AboutPanel.IsVisible = false;
        CommentEditor.IsVisible = false;
        CropOverlay.IsVisible = false;
        RefreshCropMaskVisibility(forceHidden: true);

        var display = _activeDisplay ?? GetDisplayContainingWindow();
        SetActiveDisplay(display, fullscreen: true);

        Hide();
        await Task.Delay(180);

        var number = _session.Captures.Count + 1;
        var screenshotPath = Path.Combine(_paths.CapturesFolder, $"{number:00}-screen.png");
        var captured = await _captureService.CaptureScreenAsync(display, screenshotPath);
        var thumbnailPath = Path.Combine(_paths.CapturesFolder, $"{number:00}-thumb.png");
        File.Copy(captured.ScreenshotPath, thumbnailPath, overwrite: true);

        Show();
        SetActiveDisplay(display, fullscreen: true);
        Activate();

        var capture = new CaptureViewModel(
            Guid.NewGuid().ToString("N"),
            number,
            captured.Display,
            captured.ScreenshotPath,
            thumbnailPath,
            captured.PixelSize,
            captured.Display.Bounds,
            isSelected: true);
        CaptureCropInheritancePolicy.TryCopyCrop(previousCapture, capture);

        foreach (var existing in _session.Captures)
        {
            existing.IsSelected = false;
        }

        _session.Captures.Add(capture);
        SelectCapture(capture);
        if (activateAnnotationAfterCapture)
        {
            SetAnnotationMode(true);
        }

        _isCapturing = false;
        UpdateChrome();
    }

    private async Task RequestCaptureAsync()
    {
        if (!CanUseCaptureControls())
        {
            CaptureDropdown.IsVisible = false;
            UpdateChrome();
            return;
        }

        await CaptureAsync();
    }

    private async Task ActivateCropAsync()
    {
        if (CaptureDependentToolPolicy.SelectAction(_session.CurrentCapture is not null) == CaptureDependentToolAction.CaptureFirst)
        {
            await CaptureAsync(activateAnnotationAfterCapture: false);
        }

        if (_session.CurrentCapture is null)
        {
            return;
        }

        ToggleCropOverlay();
    }

    private async Task ActivateAnnotationAsync()
    {
        var hadCapture = _session.CurrentCapture is not null;
        if (CaptureDependentToolPolicy.SelectAction(hadCapture) == CaptureDependentToolAction.CaptureFirst)
        {
            await CaptureAsync(activateAnnotationAfterCapture: false);
        }

        if (_session.CurrentCapture is null)
        {
            return;
        }

        if (!hadCapture)
        {
            SetAnnotationMode(true);
            return;
        }

        ToggleAnnotationMode();
    }

    private void ToggleAnnotationMode()
    {
        SetAnnotationMode(!_isAnnotationToggleActive);
    }

    private void SetAnnotationMode(bool isActive)
    {
        StoreCurrentCrop();
        _isAnnotationToggleActive = isActive && _session.CurrentCapture is not null;
        CommandBar.SetAnnotationActive(_isAnnotationToggleActive);

        if (!_isAnnotationToggleActive)
        {
            _isDrawing = false;
            SetChromeVisible(true);
            _session.Mode = AnnotationInteractionMode.Idle;
            CommentEditor.IsVisible = false;
            CropOverlay.IsVisible = false;
            AboutPanel.IsVisible = false;
            RefreshCropMaskVisibility();
            ApplyCurrentWindowMode();
            UpdateChrome();
            return;
        }

        _session.Mode = AnnotationInteractionMode.DrawingAnnotation;
        if (_activeDisplay is { } display)
        {
            SetActiveDisplay(display, fullscreen: true);
        }

        DisplayDropdown.IsVisible = false;
        CaptureDropdown.IsVisible = false;
        AboutPanel.IsVisible = false;
        CommentEditor.IsVisible = false;
        CropOverlay.IsVisible = false;
        RefreshCropMaskVisibility();
        UpdateChrome();
    }

    private DisplayDescriptor GetDisplayContainingWindow()
    {
        var scaling = RenderScaling > 0 ? RenderScaling : 1;
        var center = new PointInt(
            Position.X + Math.Max(1, (int)Math.Round(Bounds.Width * scaling / 2)),
            Position.Y + Math.Max(1, (int)Math.Round(Bounds.Height * scaling / 2)));
        return _displayCatalog.GetDisplayContainingPoint(center);
    }

    private bool IsBlockingSurfaceActive()
    {
        return InteractionSurfacePolicy.ShouldUseFullscreen(
            _isCapturing,
            _isDrawing,
            _session.Mode,
            CropOverlay.IsVisible,
            CommentEditor.IsVisible || IdleWarningOverlay.IsVisible,
            HasActiveCrop());
    }

    private bool ShouldRenderCaptureSurface()
    {
        return _session.CurrentCapture is not null &&
            InteractionSurfacePolicy.ShouldRenderCaptureSurface(
                _isDrawing,
                _session.Mode,
                CropOverlay.IsVisible,
                CommentEditor.IsVisible);
    }

    private void RefreshCaptureSurfaceVisibility()
    {
        var isVisible = ShouldRenderCaptureSurface();
        ScreenshotSurface.IsVisible = isVisible;
        AnnotationCanvas.IsVisible = isVisible;
        if (!isVisible)
        {
            BlurredCropMask.IsVisible = false;
        }
    }

    private void ApplyCurrentWindowMode()
    {
        if (_activeDisplay is { } display)
        {
            SetActiveDisplay(display, IsBlockingSurfaceActive());
        }
    }

    private Rect MeasureVisibleChromeFootprint()
    {
        var controls = new Control[] { CommandBar, DisplayDropdown, CaptureDropdown, AboutPanel }
            .Where(control => control.IsVisible)
            .Select(GetCanvasBounds)
            .ToList();

        return controls.Count == 0
            ? new Rect(0, 0, 1, 1)
            : controls.Aggregate((current, next) => current.Union(next));
    }

    private static Rect GetCanvasBounds(Control control)
    {
        control.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var width = control.Bounds.Width > 0 ? control.Bounds.Width : control.DesiredSize.Width;
        var height = control.Bounds.Height > 0 ? control.Bounds.Height : control.DesiredSize.Height;
        return new Rect(
            Read(Canvas.GetLeft(control)),
            Read(Canvas.GetTop(control)),
            Math.Max(1, width),
            Math.Max(1, height));
    }

    private void SelectCapture(CaptureViewModel capture)
    {
        StoreCurrentCrop();
        foreach (var existing in _session.Captures)
        {
            existing.IsSelected = existing == capture;
        }

        _session.CurrentCapture = capture;
        _session.SelectedAnnotation = null;
        ScreenshotSurface.SetImage(capture.ScreenshotPath);
        BlurredCropMask.SetImage(capture.ScreenshotPath);
        RememberCurrentViewportIfEditing(capture);
        ProjectStoredCropToViewport(capture);
        CropOverlay.SetCrop(capture.CropRect);
        CropOverlay.IsVisible = false;
        RefreshCropMaskVisibility();
        CommentEditor.IsVisible = false;
        RefreshAnnotations();
        UpdateChrome();
    }

    private void SelectCaptureForAnnotation(CaptureViewModel capture)
    {
        CaptureDropdown.IsVisible = false;
        DisplayDropdown.IsVisible = false;
        if (_activeDisplay is { } display)
        {
            SetActiveDisplay(display, fullscreen: true);
        }

        SelectCapture(capture);
        SetAnnotationMode(true);
    }

    private void DeleteCapture(CaptureViewModel capture)
    {
        var removedIndex = _session.Captures.IndexOf(capture);
        if (removedIndex < 0)
        {
            return;
        }

        var countBeforeRemoval = _session.Captures.Count;
        var wasCurrent = _session.CurrentCapture == capture;
        _session.Captures.RemoveAt(removedIndex);
        TryDeleteCaptureFiles(capture);

        if (!wasCurrent)
        {
            UpdateChrome();
            ApplyCurrentWindowMode();
            return;
        }

        var replacementIndex = CaptureRemovalPolicy.SelectReplacementIndex(countBeforeRemoval, removedIndex);
        if (replacementIndex >= 0)
        {
            SelectCapture(_session.Captures[replacementIndex]);
            if (_isAnnotationToggleActive)
            {
                SetAnnotationMode(true);
            }
            else
            {
                ApplyCurrentWindowMode();
            }

            return;
        }

        ClearCurrentCapture();
    }

    private void ClearCurrentCapture()
    {
        _session.CurrentCapture = null;
        _session.SelectedAnnotation = null;
        _commentTarget = null;
        _isDrawing = false;
        SetChromeVisible(true);
        _isAnnotationToggleActive = false;
        _session.Mode = AnnotationInteractionMode.Idle;
        CommandBar.SetAnnotationActive(false);
        ScreenshotSurface.SetImage(null);
        BlurredCropMask.SetImage(null);
        BlurredCropMask.IsVisible = false;
        CommentEditor.IsVisible = false;
        AboutPanel.IsVisible = false;
        CropOverlay.IsVisible = false;
        AnnotationCanvas.Children.Clear();
        UpdateChrome();
        ApplyCurrentWindowMode();
    }

    private static void TryDeleteCaptureFiles(CaptureViewModel capture)
    {
        TryDeleteFile(capture.ScreenshotPath);
        TryDeleteFile(capture.ThumbnailPath);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private void ToggleCaptureDropdown()
    {
        if (!CanUseCaptureControls())
        {
            CaptureDropdown.IsVisible = false;
            UpdateChrome();
            return;
        }

        CaptureDropdown.IsVisible = !CaptureDropdown.IsVisible;
        _session.Mode = CaptureDropdown.IsVisible
            ? AnnotationInteractionMode.CaptureDropdownOpen
            : AnnotationInteractionMode.Idle;
        DisplayDropdown.IsVisible = false;
        AboutPanel.IsVisible = false;
        ApplyCurrentWindowMode();
        UpdateChrome();
    }

    private void ToggleCropOverlay()
    {
        if (_session.CurrentCapture is null)
        {
            return;
        }

        var isOpening = !CropOverlay.IsVisible;
        if (isOpening)
        {
            _isAnnotationToggleActive = false;
            CommandBar.SetAnnotationActive(false);
            _session.Mode = AnnotationInteractionMode.EditingCrop;
            AboutPanel.IsVisible = false;
            if (_activeDisplay is { } display)
            {
                SetActiveDisplay(display, fullscreen: true);
            }

            RememberCurrentViewport(_session.CurrentCapture);
            ProjectStoredCropToViewport(_session.CurrentCapture);
            CropOverlay.SetCrop(_session.CurrentCapture.CropRect);
            CropOverlay.IsVisible = true;
            BlurredCropMask.SetCrop(CropOverlay.GetCrop());
        }
        else
        {
            StoreCurrentCrop();
            CropOverlay.IsVisible = false;
            _session.Mode = AnnotationInteractionMode.Idle;
            ApplyCurrentWindowMode();
        }

        RefreshCropMaskVisibility();
        UpdateChrome();
    }

    private void RefreshCropMaskVisibility(bool forceHidden = false)
    {
        if (forceHidden || _session.CurrentCapture is null || !ShouldRenderCaptureSurface())
        {
            BlurredCropMask.IsVisible = false;
            return;
        }

        if (CropOverlay.IsVisible)
        {
            RememberCurrentViewport(_session.CurrentCapture);
            _session.CurrentCapture.CropRect = CropOverlay.GetCrop();
        }
        else if (IsBlockingSurfaceActive())
        {
            RememberCurrentViewport(_session.CurrentCapture);
            ProjectStoredCropToViewport(_session.CurrentCapture);
        }

        BlurredCropMask.SetCrop(_session.CurrentCapture.CropRect);
        BlurredCropMask.IsVisible = IsBlockingSurfaceActive() &&
            (CropOverlay.IsVisible || CaptureCropProjector.IsCropped(_session.CurrentCapture));
    }

    private void StoreCurrentCrop()
    {
        if (_session.CurrentCapture is not null && CropOverlay.IsVisible)
        {
            RememberCurrentViewport(_session.CurrentCapture);
            CaptureCropProjector.CommitViewportCrop(_session.CurrentCapture, CropOverlay.GetCrop());
            UpdateAnnotationExportStates(_session.CurrentCapture);
        }
    }

    private static void ProjectStoredCropToViewport(CaptureViewModel capture)
    {
        capture.CropRect = CaptureCropProjector.ToViewportCrop(capture);
    }

    private void RememberCurrentViewportIfEditing(CaptureViewModel capture)
    {
        if (_session.Mode is AnnotationInteractionMode.Editing or
            AnnotationInteractionMode.DrawingAnnotation or
            AnnotationInteractionMode.EditingCrop or
            AnnotationInteractionMode.AnnotationSelected)
        {
            RememberCurrentViewport(capture);
        }
    }

    private void RememberCurrentViewport(CaptureViewModel capture)
    {
        capture.ViewportSize = GetCurrentViewportSize(capture);
    }

    private SizeInt GetCurrentViewportSize(CaptureViewModel capture)
    {
        return ViewportSizeSelector.Select(
            Width,
            Height,
            Bounds.Width,
            Bounds.Height,
            AnnotationCanvas.Bounds.Width,
            AnnotationCanvas.Bounds.Height,
            capture.ScreenshotPixelSize);
    }

    private bool HasActiveCrop()
    {
        return _session.CurrentCapture is { } capture &&
            CaptureCropProjector.IsCropped(capture);
    }

    private void OnAnnotationPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_session.CurrentCapture is null || _session.Mode != AnnotationInteractionMode.DrawingAnnotation)
        {
            return;
        }

        _isDrawing = true;
        SetChromeVisible(false);
        _drawStart = e.GetPosition(AnnotationCanvas);
        _draftBox = new Border
        {
            BorderBrush = App.Current?.FindResource("AnnotationStrokeBrush") as Avalonia.Media.IBrush,
            Background = App.Current?.FindResource("AnnotationBrush") as Avalonia.Media.IBrush,
            BorderThickness = new Thickness(2)
        };
        Canvas.SetLeft(_draftBox, _drawStart.X);
        Canvas.SetTop(_draftBox, _drawStart.Y);
        AnnotationCanvas.Children.Add(_draftBox);
        _draftWarning = CreateDraftWarning();
        AnnotationCanvas.Children.Add(_draftWarning);
        PositionDraftWarning(new RectInt((int)Math.Round(_drawStart.X), (int)Math.Round(_drawStart.Y), 0, 0));
        e.Pointer.Capture(AnnotationCanvas);
    }

    private void OnAnnotationPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDrawing || _draftBox is null)
        {
            return;
        }

        var current = e.GetPosition(AnnotationCanvas);
        var rect = CreateAnnotationRectFromDrag(current);
        _draftBox.Width = rect.Width;
        _draftBox.Height = rect.Height;
        Canvas.SetLeft(_draftBox, rect.X);
        Canvas.SetTop(_draftBox, rect.Y);
        UpdateDraftBoxVisual(rect);
        PositionDraftWarning(rect);
    }

    private void OnAnnotationPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDrawing || _draftBox is null || _session.CurrentCapture is null)
        {
            return;
        }

        _isDrawing = false;
        e.Pointer.Capture(null);
        var rect = CreateAnnotationRectFromDrag(e.GetPosition(AnnotationCanvas));

        AnnotationCanvas.Children.Remove(_draftBox);
        _draftBox = null;
        if (_draftWarning is not null)
        {
            AnnotationCanvas.Children.Remove(_draftWarning);
            _draftWarning = null;
        }

        SetChromeVisible(true);

        if (!AnnotationRectPolicy.IsMinimumSizeReached(rect, MinimumAnnotationSize))
        {
            return;
        }

        var annotation = new AnnotationViewModel(
            Guid.NewGuid().ToString("N"),
            _session.CurrentCapture.GetNextAnnotationNumber(),
            rect,
            string.Empty,
            isPendingComment: true);

        _session.CurrentCapture.Annotations.Add(annotation);
        SelectAnnotation(annotation);
        RefreshAnnotations();
    }

    private void RefreshAnnotations()
    {
        AnnotationCanvas.Children.Clear();
        if (_session.CurrentCapture is null)
        {
            return;
        }

        UpdateAnnotationExportStates(_session.CurrentCapture);
        foreach (var annotation in _session.CurrentCapture.Annotations.OrderBy(item => item.Number))
        {
            var box = new AnnotationBoxControl();
            box.SetAnnotation(annotation);
            box.Selected += (_, request) => SelectAnnotation(request);
            box.RectChanged += (_, _) =>
            {
                PositionCommentEditor(annotation);
                if (_session.CurrentCapture is { } currentCapture)
                {
                    UpdateAnnotationExportState(currentCapture, annotation);
                }
            };
            Canvas.SetLeft(box, annotation.BoxRect.X);
            Canvas.SetTop(box, annotation.BoxRect.Y);
            AnnotationCanvas.Children.Add(box);
        }
    }

    private void UpdateAnnotationExportStates(CaptureViewModel capture)
    {
        RememberCurrentViewport(capture);
        foreach (var annotation in capture.Annotations)
        {
            UpdateAnnotationExportState(capture, annotation);
        }
    }

    private void UpdateAnnotationExportState(CaptureViewModel capture, AnnotationViewModel annotation)
    {
        var crop = ClampCrop(capture.CropPixelRect, capture.ScreenshotPixelSize);
        var pixelBox = ToPixelRect(annotation.BoxRect, capture);
        annotation.ExportState = AnnotationCropPolicy.Classify(pixelBox, crop).State;
    }

    private void SelectAnnotation(AnnotationViewModel annotation)
    {
        if (_session.CurrentCapture is null)
        {
            return;
        }

        foreach (var existing in _session.CurrentCapture.Annotations)
        {
            existing.IsSelected = existing == annotation;
        }

        _session.SelectedAnnotation = annotation;
        _session.Mode = AnnotationInteractionMode.AnnotationSelected;
        _commentTarget = annotation;
        CommentEditor.IsVisible = true;
        CommentEditor.Open(annotation.Comment);
        PositionCommentEditor(annotation);
        RefreshCropMaskVisibility();
        UpdateChrome();
    }

    private void SelectAnnotation(AnnotationSelectionRequest request)
    {
        if (_session.CurrentCapture is null)
        {
            return;
        }

        var selected = AnnotationSelectionPolicy.SelectAtPoint(
            _session.CurrentCapture.Annotations,
            _session.SelectedAnnotation,
            request.Annotation,
            request.Point);
        SelectAnnotation(selected);
    }

    private void PositionCommentEditor(AnnotationViewModel annotation)
    {
        CommentEditor.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var editorWidth = Math.Max(620, CommentEditor.DesiredSize.Width);
        var editorHeight = Math.Max(108, CommentEditor.DesiredSize.Height);
        var x = Math.Min(Math.Max(16, annotation.BoxRect.X + 12), Math.Max(16, Bounds.Width - editorWidth - 16));
        var y = Math.Min(Math.Max(16, annotation.BoxRect.Y + annotation.BoxRect.Height + 10), Math.Max(16, Bounds.Height - editorHeight - 16));
        Canvas.SetLeft(CommentEditor, x);
        Canvas.SetTop(CommentEditor, y);
    }

    private void DeleteCommentTarget()
    {
        if (_session.CurrentCapture is null || _commentTarget is null)
        {
            return;
        }

        _session.CurrentCapture.Annotations.Remove(_commentTarget);
        _commentTarget = null;
        CommentEditor.IsVisible = false;
        ReturnAfterCommentEdit();
        RefreshAnnotations();
    }

    private void CancelCommentTarget()
    {
        if (_commentTarget is null)
        {
            return;
        }

        if (_session.CurrentCapture is not null &&
            CommentEditCancelPolicy.SelectAction(_commentTarget.IsPendingComment) == CommentEditCancelAction.DeleteAnnotation)
        {
            _session.CurrentCapture.Annotations.Remove(_commentTarget);
            _commentTarget = null;
            _session.SelectedAnnotation = null;
            CommentEditor.IsVisible = false;
            ReturnAfterCommentEdit();
            RefreshAnnotations();
            return;
        }

        _commentTarget.IsSelected = false;
        _session.SelectedAnnotation = null;
        _commentTarget = null;
        CommentEditor.IsVisible = false;
        ReturnAfterCommentEdit();
        RefreshAnnotations();
    }

    private void SaveCommentTarget(string text)
    {
        if (_commentTarget is null)
        {
            return;
        }

        _commentTarget.Comment = text.Trim();
        _commentTarget.IsPendingComment = false;
        _commentTarget.IsSelected = false;
        _session.SelectedAnnotation = null;
        CommentEditor.IsVisible = false;
        _commentTarget = null;
        ReturnAfterCommentEdit();
    }

    private void ReturnAfterCommentEdit()
    {
        if (_isAnnotationToggleActive && _session.CurrentCapture is not null)
        {
            _session.Mode = AnnotationInteractionMode.DrawingAnnotation;
            CommentEditor.IsVisible = false;
            if (_activeDisplay is { } display)
            {
                SetActiveDisplay(display, fullscreen: true);
            }

            RefreshCropMaskVisibility();
            UpdateChrome();
            return;
        }

        _session.Mode = AnnotationInteractionMode.Idle;
        ApplyCurrentWindowMode();
        UpdateChrome();
    }

    private async Task FinishAsync()
    {
        if (_paths is null)
        {
            return;
        }

        StoreCurrentCrop();
        StoreActiveComment();
        StopIdleTimers();
        var session = BuildExportSession();
        await _exporter.ExportAsync(_paths, session);
        DeleteCroppedCaptureSourceFiles();
        await _store.MarkCompletedAsync(_paths, "review.md", "annotations.json");
        _hasTerminalStatus = true;
        Close();
    }

    private void DeleteCroppedCaptureSourceFiles()
    {
        foreach (var capture in _session.Captures.Where(CaptureCropProjector.IsCropped))
        {
            TryDeleteFile(capture.ScreenshotPath);
            TryDeleteFile(capture.ThumbnailPath);
        }
    }

    private async Task CancelAsync()
    {
        StopIdleTimers();
        if (_paths is not null)
        {
            await _store.MarkCancelledAsync(_paths);
        }

        _hasTerminalStatus = true;
        Close();
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_hasTerminalStatus || _paths is null)
        {
            return;
        }

        StopIdleTimers();
        _hasTerminalStatus = true;
        await _store.MarkCancelledAsync(_paths);
    }

    private void StopIdleTimers()
    {
        _idleTimer?.Stop();
        _idleWarningTimer?.Stop();
    }

    private void StoreActiveComment()
    {
        if (_commentTarget is not null && CommentEditor.IsVisible)
        {
            _commentTarget.Comment = CommentEditor.CurrentText.Trim();
            _commentTarget.IsPendingComment = false;
        }
    }

    private AnnotationSession BuildExportSession()
    {
        var created = _status?.CreatedAtUtc ?? DateTimeOffset.UtcNow;
        return new AnnotationSession(
            _status?.SessionId ?? Guid.NewGuid().ToString("N"),
            created,
            DateTimeOffset.UtcNow,
            SessionStatus.Completed,
            _session.Captures.Select(ToCaptureModel).ToList());
    }

    private AnnotationCapture ToCaptureModel(CaptureViewModel capture)
    {
        var cropRect = ClampCrop(capture.CropPixelRect, capture.ScreenshotPixelSize);
        var cropPath = WriteCropIfNeeded(capture, cropRect);
        return new AnnotationCapture(
            capture.CaptureId,
            capture.Number,
            new CaptureDisplay(capture.Display.Id, capture.Display.Name, capture.Display.Bounds),
            capture.ScreenshotPath,
            cropPath,
            capture.ThumbnailPath,
            capture.ScreenshotPixelSize,
            capture.ScreenBounds,
            cropRect,
            capture.Annotations
                .OrderBy(annotation => annotation.Number)
                .Select(annotation => new Annotation(
                    annotation.AnnotationId,
                    annotation.Number,
                    ToPixelRect(annotation.BoxRect, capture),
                    annotation.Comment))
                .ToList());
    }

    private RectInt ToPixelRect(RectInt viewRect, CaptureViewModel capture)
    {
        return CaptureCoordinateMapper.ToPixelRect(viewRect, capture);
    }

    private string? WriteCropIfNeeded(CaptureViewModel capture, RectInt cropRect)
    {
        if (_paths is null ||
            cropRect.X == 0 &&
            cropRect.Y == 0 &&
            cropRect.Width == capture.ScreenshotPixelSize.Width &&
            cropRect.Height == capture.ScreenshotPixelSize.Height)
        {
            return null;
        }

        var crop = ClampCrop(cropRect, capture.ScreenshotPixelSize);
        var cropPath = Path.Combine(_paths.CapturesFolder, $"{capture.Number:00}-crop.png");
        using var source = new DrawingBitmap(capture.ScreenshotPath);
        using var target = new DrawingBitmap(crop.Width, crop.Height);
        using var graphics = DrawingGraphics.FromImage(target);
        graphics.DrawImage(
            source,
            new DrawingRectangle(0, 0, crop.Width, crop.Height),
            new DrawingRectangle(crop.X, crop.Y, crop.Width, crop.Height),
            DrawingGraphicsUnit.Pixel);
        target.Save(cropPath, ImageFormat.Png);
        return cropPath;
    }

    private static RectInt ClampCrop(RectInt crop, SizeInt size)
    {
        var x = Math.Clamp(crop.X, 0, size.Width - 1);
        var y = Math.Clamp(crop.Y, 0, size.Height - 1);
        var width = Math.Clamp(crop.Width, 1, size.Width - x);
        var height = Math.Clamp(crop.Height, 1, size.Height - y);
        return new RectInt(x, y, width, height);
    }

    private void UpdateChrome()
    {
        RefreshCaptureSurfaceVisibility();
        var canUseCaptureControls = CanUseCaptureControls();
        CommandBar.SetCaptureNumber(_session.CurrentCapture?.Number ?? 0);
        CommandBar.SetCaptureControlsEnabled(canUseCaptureControls);
        CaptureDropdown.SetCaptures(_session.Captures);
        CaptureDropdown.SetCanCreateCapture(canUseCaptureControls);
        DisplayDropdown.SetDisplays(CreateDisplayViewModels());
    }

    private bool CanUseCaptureControls()
    {
        return CaptureCreationPolicy.CanUseCaptureControls(_session.Mode, CropOverlay.IsVisible);
    }

    private IReadOnlyList<DisplayViewModel> CreateDisplayViewModels()
    {
        var current = _activeDisplay ?? GetDisplayContainingWindow();
        var displays = _displayCatalog.GetDisplays();
        var displayNumbers = DisplaySettingsNumberAssigner.Assign(displays
            .Select((display, index) => new DisplaySettingsNumberSource(index, display.Bounds, display.IsPrimary))
            .ToList());

        return displays
            .Select((display, index) => new DisplayViewModel(
                displayNumbers[index],
                display,
                string.Equals(display.Id, current.Id, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private static double Read(double value)
    {
        return double.IsNaN(value) ? 0 : value;
    }

    private void SetChromeVisible(bool isVisible)
    {
        ChromeCanvas.IsVisible = isVisible;
    }

    private SizeInt GetAnnotationCanvasSize()
    {
        return new SizeInt(
            Math.Max(1, (int)Math.Round(AnnotationCanvas.Bounds.Width > 0 ? AnnotationCanvas.Bounds.Width : Bounds.Width)),
            Math.Max(1, (int)Math.Round(AnnotationCanvas.Bounds.Height > 0 ? AnnotationCanvas.Bounds.Height : Bounds.Height)));
    }

    private RectInt CreateAnnotationRectFromDrag(Point current)
    {
        return AnnotationRectPolicy.CreateFromDrag(
            new PointInt((int)Math.Round(_drawStart.X), (int)Math.Round(_drawStart.Y)),
            new PointInt((int)Math.Round(current.X), (int)Math.Round(current.Y)),
            GetAnnotationCanvasSize());
    }

    private static string CreateAboutVersionText()
    {
        var version = typeof(MainWindow).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        if (string.IsNullOrWhiteSpace(version))
        {
            version = typeof(MainWindow).Assembly.GetName().Version?.ToString() ?? "unknown";
        }

        return $"v{version.Split('+')[0]}";
    }

    private void UpdateDraftBoxVisual(RectInt rect)
    {
        if (_draftBox is null)
        {
            return;
        }

        var isValid = AnnotationRectPolicy.IsMinimumSizeReached(rect, MinimumAnnotationSize);
        _draftBox.BorderBrush = App.Current?.FindResource(isValid ? "AnnotationStrokeBrush" : "InvalidAnnotationStrokeBrush") as Avalonia.Media.IBrush;
        _draftBox.Background = App.Current?.FindResource(isValid ? "AnnotationBrush" : "InvalidAnnotationBrush") as Avalonia.Media.IBrush;
    }

    private static Border CreateDraftWarning()
    {
        return new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(218, 31, 41, 55)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 4),
            Child = new TextBlock
            {
                Text = $"Min {AnnotationRectPolicy.MinimumSize} x {AnnotationRectPolicy.MinimumSize} px",
                Foreground = Brushes.White,
                FontSize = 12,
                FontWeight = FontWeight.SemiBold
            }
        };
    }

    private void PositionDraftWarning(RectInt rect)
    {
        if (_draftWarning is null)
        {
            return;
        }

        _draftWarning.IsVisible = !AnnotationRectPolicy.IsMinimumSizeReached(rect, MinimumAnnotationSize);
        if (!_draftWarning.IsVisible)
        {
            return;
        }

        _draftWarning.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var warningWidth = Math.Max(1, _draftWarning.DesiredSize.Width);
        var warningHeight = Math.Max(1, _draftWarning.DesiredSize.Height);
        var bounds = GetAnnotationCanvasSize();
        var x = Math.Clamp(rect.X + rect.Width + 8, 0, Math.Max(0, bounds.Width - (int)Math.Ceiling(warningWidth)));
        var y = Math.Clamp(rect.Y - (int)Math.Ceiling(warningHeight) - 6, 0, Math.Max(0, bounds.Height - (int)Math.Ceiling(warningHeight)));

        Canvas.SetLeft(_draftWarning, x);
        Canvas.SetTop(_draftWarning, y);
    }
}
