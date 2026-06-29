using System.Drawing.Imaging;
using System.Text.Json;
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
    private const int MinimumAnnotationSize = 12;
    private static readonly TimeSpan ActivityWriteInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan IdleWarningDuration = TimeSpan.FromSeconds(30);
    private readonly AnnotationSessionViewModel _session = new();
    private readonly SessionStore _store = new();
    private readonly SessionExporter _exporter = new();
    private readonly IDisplayCatalog _displayCatalog = new WindowsDisplayCatalog();
    private readonly IScreenCaptureService _captureService = new WindowsScreenCaptureService();
    private readonly TimeSpan? _idleTimeout;
    private readonly string? _providedSessionFolder;
    private SessionPaths? _paths;
    private SessionStatusDocument? _status;
    private bool _isCapturing;
    private bool _isDrawing;
    private bool _hasTerminalStatus;
    private bool _isAnnotationToggleActive;
    private DisplayDescriptor? _activeDisplay;
    private Point _drawStart;
    private Border? _draftBox;
    private AnnotationViewModel? _commentTarget;
    private DateTimeOffset _lastActivityWriteUtc = DateTimeOffset.MinValue;
    private DateTimeOffset _lastUserActivityUtc = DateTimeOffset.UtcNow;
    private DateTimeOffset _idleWarningExpiresAtUtc;
    private DispatcherTimer? _idleTimer;
    private DispatcherTimer? _idleWarningTimer;

    public MainWindow()
        : this(null)
    {
    }

    public MainWindow(string? sessionFolder)
        : this(sessionFolder, null)
    {
    }

    public MainWindow(string? sessionFolder, TimeSpan? idleTimeout)
    {
        _providedSessionFolder = sessionFolder;
        _idleTimeout = idleTimeout;
        InitializeComponent();
        Opened += OnOpened;
        Closing += OnClosing;
        AnnotationCanvas.PointerPressed += OnAnnotationPointerPressed;
        AnnotationCanvas.PointerMoved += OnAnnotationPointerMoved;
        AnnotationCanvas.PointerReleased += OnAnnotationPointerReleased;
        CommandBar.MoveSelectorRequested += (_, _) => ToggleDisplayDropdown();
        CommandBar.CaptureRequested += async (_, _) => await CaptureAsync();
        CommandBar.CaptureSelectorRequested += (_, _) => ToggleCaptureDropdown();
        CommandBar.CropRequested += async (_, _) => await ActivateCropAsync();
        CommandBar.AnnotationRequested += async (_, _) => await ActivateAnnotationAsync();
        CommandBar.FinishRequested += async (_, _) => await FinishAsync();
        CommandBar.CancelRequested += async (_, _) => await CancelAsync();
        DisplayDropdown.DisplaySelected += (_, display) => MoveToDisplay(display.Display);
        CaptureDropdown.CaptureSelected += (_, capture) => SelectCaptureForAnnotation(capture);
        CaptureDropdown.NewCaptureRequested += async (_, _) => await CaptureAsync();
        CommentEditor.DeleteRequested += (_, _) => DeleteCommentTarget();
        CommentEditor.SaveRequested += (_, text) => SaveCommentTarget(text);
        CropOverlay.CropChanged += (_, crop) => BlurredCropMask.SetCrop(crop);
        IdleWarningContinueButton.Click += (_, _) => ContinueAfterIdleWarning();
        IdleWarningDiscardButton.Click += async (_, _) => await CancelAsync();
        IdleWarningSendButton.Click += async (_, _) => await FinishAsync();
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

    private async Task EnsureSessionAsync()
    {
        if (string.IsNullOrWhiteSpace(_providedSessionFolder))
        {
            _paths = await _store.CreateSessionAsync(null);
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
        _isCapturing = true;
        DisplayDropdown.IsVisible = false;
        CaptureDropdown.IsVisible = false;
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
            _session.Mode = AnnotationInteractionMode.Idle;
            CommentEditor.IsVisible = false;
            CropOverlay.IsVisible = false;
            RefreshCropMaskVisibility();
            ApplyCurrentWindowMode();
            return;
        }

        _session.Mode = AnnotationInteractionMode.DrawingAnnotation;
        if (_activeDisplay is { } display)
        {
            SetActiveDisplay(display, fullscreen: true);
        }

        DisplayDropdown.IsVisible = false;
        CaptureDropdown.IsVisible = false;
        CommentEditor.IsVisible = false;
        CropOverlay.IsVisible = false;
        RefreshCropMaskVisibility();
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

    private void ApplyCurrentWindowMode()
    {
        if (_activeDisplay is { } display)
        {
            SetActiveDisplay(display, IsBlockingSurfaceActive());
        }
    }

    private Rect MeasureVisibleChromeFootprint()
    {
        var controls = new Control[] { CommandBar, DisplayDropdown, CaptureDropdown }
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

    private void ToggleCaptureDropdown()
    {
        CaptureDropdown.IsVisible = !CaptureDropdown.IsVisible;
        DisplayDropdown.IsVisible = false;
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
    }

    private void RefreshCropMaskVisibility(bool forceHidden = false)
    {
        if (forceHidden || _session.CurrentCapture is null)
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
        e.Pointer.Capture(AnnotationCanvas);
    }

    private void OnAnnotationPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDrawing || _draftBox is null)
        {
            return;
        }

        var current = e.GetPosition(AnnotationCanvas);
        var left = Math.Min(_drawStart.X, current.X);
        var top = Math.Min(_drawStart.Y, current.Y);
        _draftBox.Width = Math.Abs(current.X - _drawStart.X);
        _draftBox.Height = Math.Abs(current.Y - _drawStart.Y);
        Canvas.SetLeft(_draftBox, left);
        Canvas.SetTop(_draftBox, top);
    }

    private void OnAnnotationPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDrawing || _draftBox is null || _session.CurrentCapture is null)
        {
            return;
        }

        _isDrawing = false;
        e.Pointer.Capture(null);
        var rect = new RectInt(
            Math.Max(0, (int)Math.Round(Read(Canvas.GetLeft(_draftBox)))),
            Math.Max(0, (int)Math.Round(Read(Canvas.GetTop(_draftBox)))),
            Math.Max(1, (int)Math.Round(_draftBox.Width)),
            Math.Max(1, (int)Math.Round(_draftBox.Height)));

        AnnotationCanvas.Children.Remove(_draftBox);
        _draftBox = null;

        if (rect.Width < MinimumAnnotationSize || rect.Height < MinimumAnnotationSize)
        {
            return;
        }

        var annotation = new AnnotationViewModel(
            Guid.NewGuid().ToString("N"),
            _session.CurrentCapture.GetNextAnnotationNumber(),
            rect,
            string.Empty);

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

        foreach (var annotation in _session.CurrentCapture.Annotations.OrderBy(item => item.Number))
        {
            var box = new AnnotationBoxControl();
            box.SetAnnotation(annotation);
            box.Selected += (_, selected) => SelectAnnotation(selected);
            box.RectChanged += (_, _) => PositionCommentEditor(annotation);
            Canvas.SetLeft(box, annotation.BoxRect.X);
            Canvas.SetTop(box, annotation.BoxRect.Y);
            AnnotationCanvas.Children.Add(box);
        }
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
    }

    private void PositionCommentEditor(AnnotationViewModel annotation)
    {
        CommentEditor.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var editorWidth = Math.Max(456, CommentEditor.DesiredSize.Width);
        var editorHeight = Math.Max(58, CommentEditor.DesiredSize.Height);
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

    private void SaveCommentTarget(string text)
    {
        if (_commentTarget is null)
        {
            return;
        }

        _commentTarget.Comment = text.Trim();
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
            return;
        }

        _session.Mode = AnnotationInteractionMode.Idle;
        ApplyCurrentWindowMode();
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
        await _store.MarkCompletedAsync(_paths, "review.md", "annotations.json");
        _hasTerminalStatus = true;
        Close();
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
        CommandBar.SetCaptureNumber(_session.CurrentCapture?.Number ?? 0);
        CaptureDropdown.SetCaptures(_session.Captures);
        DisplayDropdown.SetDisplays(CreateDisplayViewModels());
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
}
