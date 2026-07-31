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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

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
    private readonly IDisplayCatalog _displayCatalog;
    private readonly IScreenCaptureService _captureService;
    private readonly IWindowIntegration _windowIntegration;
    private readonly ToolbarWindow _toolbarWindow;
    private readonly ToolbarPlacementController _toolbarPlacementController;
    private readonly OverlayWindowCoordinator _windowCoordinator;
    private readonly PortableImageCropWriter _cropWriter = new();
    private readonly CapturePreviewWriter _previewWriter = new();
    private readonly IExportDestinationPicker _exportDestinationPicker = new ExportDestinationPicker();
    private readonly LaunchCaller _launchCaller;
    private readonly int _defaultScalePercent;
    private readonly TimeSpan? _idleTimeout;
    private readonly string? _providedSessionFolder;
    private readonly string? _providedExportFolder;
    private readonly string? _providedSessionRoot;
    private readonly string? _providedExportRoot;
    private SessionPaths? _paths;
    private SessionStatusDocument? _status;
    private bool _isCapturing;
    private bool _isDrawing;
    private bool _hasTerminalStatus;
    private bool _isAnnotationToggleActive;
    private bool _isPrivacyMaskToggleActive;
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
    private DispatcherTimer? _captureHeartbeatTimer;
    private CancellationTokenSource? _captureCancellation;
    private TaskCompletionSource<bool>? _sessionConfirmation;
    private bool _isFinishing;
    private bool _isUiInitialized;
    private bool _isFinalClose;

    public MainWindow()
        : this(null, null, null, null, null, CaptureScale.DefaultPercent, LaunchCaller.Human)
    {
    }

    public MainWindow(string? sessionFolder)
        : this(sessionFolder, null, null, null, null, CaptureScale.DefaultPercent, LaunchCaller.Human)
    {
    }

    public MainWindow(string? sessionFolder, TimeSpan? idleTimeout)
        : this(sessionFolder, null, null, null, idleTimeout, CaptureScale.DefaultPercent, LaunchCaller.Human)
    {
    }

    public MainWindow(
        string? sessionFolder,
        string? exportFolder,
        string? sessionRoot,
        string? exportRoot,
        TimeSpan? idleTimeout)
        : this(sessionFolder, exportFolder, sessionRoot, exportRoot, idleTimeout, CaptureScale.DefaultPercent, LaunchCaller.Human)
    {
    }

    public MainWindow(
        string? sessionFolder,
        string? exportFolder,
        string? sessionRoot,
        string? exportRoot,
        TimeSpan? idleTimeout,
        int defaultScalePercent,
        LaunchCaller launchCaller)
    {
        var platformServices = AppPlatformServices.Create();
        _displayCatalog = new AvaloniaDisplayCatalog(() => Screens);
        _captureService = platformServices.ScreenCapture;
        _windowIntegration = platformServices.WindowIntegration;
        _providedSessionFolder = sessionFolder;
        _providedExportFolder = exportFolder;
        _providedSessionRoot = sessionRoot;
        _providedExportRoot = exportRoot;
        _idleTimeout = idleTimeout;
        _defaultScalePercent = CaptureScale.Clamp(defaultScalePercent);
        _launchCaller = launchCaller;
        InitializeComponent();
        _toolbarWindow = new ToolbarWindow();
        _toolbarPlacementController = new ToolbarPlacementController(
            _toolbarWindow,
            new UiSettingsStore());
        _windowCoordinator = new OverlayWindowCoordinator(
            this,
            _toolbarWindow,
            _windowIntegration,
            _toolbarPlacementController.ClampToVisibleArea);
        Opened += OnOpened;
        Closing += OnClosing;
        _toolbarWindow.Closing += OnToolbarClosing;
        AnnotationCanvas.AddHandler(
            PointerPressedEvent,
            OnAnnotationPointerPressedTunnel,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
        AnnotationCanvas.PointerPressed += OnAnnotationPointerPressed;
        AnnotationCanvas.PointerMoved += OnAnnotationPointerMoved;
        AnnotationCanvas.PointerReleased += OnAnnotationPointerReleased;
        CommandBar.MoveSelectorRequested += (_, _) => ToggleDisplayDropdown();
        CommandBar.CaptureRequested += async (_, _) => await RequestCaptureAsync();
        CommandBar.CaptureSelectorRequested += (_, _) => ToggleCaptureDropdown();
        CommandBar.CropRequested += async (_, _) => await ActivateCropAsync();
        CommandBar.PrivacyMaskRequested += async (_, _) => await ActivatePrivacyMaskAsync();
        CommandBar.AnnotationRequested += async (_, _) => await ActivateAnnotationAsync();
        CaptureScaleSelector.ScaleChanged += (_, percent) => SetCurrentCaptureScalePercent(percent);
        CommandBar.FinishRequested += async (_, _) => await FinishAsync();
        CommandBar.AboutRequested += (_, _) => ToggleAboutPanel();
        CommandBar.CancelRequested += async (_, _) => await RequestCancelAsync();
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
        SessionConfirmationCancelButton.Click += (_, _) => CompleteSessionConfirmation(confirmed: false);
        SessionConfirmationConfirmButton.Click += (_, _) => CompleteSessionConfirmation(confirmed: true);
        SessionConfirmationOverlay.KeyDown += OnSessionConfirmationKeyDown;
        AboutCloseButton.Click += (_, _) => CloseAboutPanel();
        CaptureStatusDismissButton.Click += (_, _) => CloseCaptureStatusFeedback();
        AboutGitHubLinkText.PointerPressed += (_, _) => OpenGitHubRepository();
        CommandBar.SetAgentCompletion(_launchCaller == LaunchCaller.Agent);
        IdleWarningSendButton.Content = _launchCaller == LaunchCaller.Agent ? "Send" : "Export";
        RegisterUserActivityHandlers(this);
        RegisterUserActivityHandlers(_toolbarWindow);
    }

    private FloatingCommandBar CommandBar => _toolbarWindow.CommandBarView;

    private CaptureScaleSelector CaptureScaleSelector => _toolbarWindow.CaptureScaleSelectorView;

    private DisplayDropdown DisplayDropdown => _toolbarWindow.DisplayDropdownView;

    private CaptureDropdown CaptureDropdown => _toolbarWindow.CaptureDropdownView;

    private Button AboutCloseButton => _toolbarWindow.AboutCloseButton;

    private TextBlock AboutVersionText => _toolbarWindow.AboutVersionText;

    private TextBlock AboutGitHubLinkText => _toolbarWindow.AboutGitHubLinkText;

    private Button CaptureStatusDismissButton => _toolbarWindow.CaptureStatusDismissButton;

    private TextBlock CaptureStatusTitleText => _toolbarWindow.CaptureStatusTitleText;

    private TextBlock CaptureStatusMessageText => _toolbarWindow.CaptureStatusMessageText;

    private void RegisterUserActivityHandlers(Interactive source)
    {
        source.AddHandler(PointerPressedEvent, OnUserActivity, RoutingStrategies.Tunnel, handledEventsToo: true);
        source.AddHandler(PointerMovedEvent, OnUserActivity, RoutingStrategies.Tunnel, handledEventsToo: true);
        source.AddHandler(PointerReleasedEvent, OnUserActivity, RoutingStrategies.Tunnel, handledEventsToo: true);
        source.AddHandler(PointerWheelChangedEvent, OnUserActivity, RoutingStrategies.Tunnel, handledEventsToo: true);
        source.AddHandler(KeyDownEvent, OnUserActivity, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (_isUiInitialized)
        {
            return;
        }

        _isUiInitialized = true;
        _windowCoordinator.InitializeToolbar();
        PlaceOnPrimaryDisplay();
        await _toolbarPlacementController.InitializeAsync();
        _windowCoordinator.RevealToolbar();
        await EnsureSessionAsync();
        AboutVersionText.Text = CreateAboutVersionText();
        ResetIdleTimer();
        UpdateChrome();
        CommandBar.PlayStartupAttentionAnimation();
    }

    private bool IsDisplayDropdownOpen
    {
        get => _toolbarWindow.IsDisplayDropdownOpen;
        set => _toolbarWindow.IsDisplayDropdownOpen = value;
    }

    private bool IsCaptureDropdownOpen
    {
        get => _toolbarWindow.IsCaptureDropdownOpen;
        set => _toolbarWindow.IsCaptureDropdownOpen = value;
    }

    private bool IsAboutPanelOpen
    {
        get => _toolbarWindow.IsAboutPanelOpen;
        set => _toolbarWindow.IsAboutPanelOpen = value;
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
            _paths = await _store.CreateSessionAsync(_providedSessionRoot, _providedExportRoot);
            _status = await _store.ReadStatusAsync(_paths);
            return;
        }

        Directory.CreateDirectory(_providedSessionFolder);
        Directory.CreateDirectory(Path.Combine(_providedSessionFolder, "captures"));
        var exportFolder = _providedExportFolder ?? CreateDefaultExportFolder(_providedSessionFolder);
        Directory.CreateDirectory(exportFolder);
        Directory.CreateDirectory(Path.Combine(exportFolder, "captures"));
        _paths = SessionPaths.FromFolder(_providedSessionFolder, exportFolder);
        _status = File.Exists(_paths.StatusJsonPath)
            ? await _store.ReadStatusAsync(_paths)
            : await InitializeProvidedSessionAsync(_paths);
    }

    private static string CreateDefaultExportFolder(string sessionFolder)
    {
        var sessionId = Path.GetFileName(sessionFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return Path.Combine(Path.GetTempPath(), "AA.Annotate", "exports", sessionId);
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
        if (!IdleWarningOverlay.IsVisible && !SessionConfirmationOverlay.IsVisible)
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
        return _session.Captures.Any(capture => capture.Annotations.Count > 0 || capture.PrivacyMasks.Count > 0);
    }

    private void PlaceOnPrimaryDisplay()
    {
        var display = _displayCatalog.GetDisplays().FirstOrDefault(screen => screen.IsPrimary)
            ?? _displayCatalog.GetDisplays().First();
        SetActiveDisplay(display, fullscreen: false);
    }

    private void ToggleDisplayDropdown()
    {
        if (_isCapturing)
        {
            return;
        }

        IsDisplayDropdownOpen = !IsDisplayDropdownOpen;
        IsCaptureDropdownOpen = false;
        IsAboutPanelOpen = false;
        ApplyCurrentWindowMode();
        UpdateChrome();
    }

    private void ToggleAboutPanel()
    {
        if (_isCapturing)
        {
            return;
        }

        IsAboutPanelOpen = !IsAboutPanelOpen;
        IsDisplayDropdownOpen = false;
        IsCaptureDropdownOpen = false;
        CommentEditor.IsVisible = false;
        ApplyCurrentWindowMode();
        UpdateChrome();
    }

    private void CloseAboutPanel()
    {
        IsAboutPanelOpen = false;
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
        IsDisplayDropdownOpen = false;
        IsAboutPanelOpen = false;
        CommentEditor.IsVisible = false;
        CropOverlay.IsVisible = false;
        SetActiveDisplay(display, IsBlockingSurfaceActive());
        RefreshCropMaskVisibility();
        UpdateChrome();
    }

    private void SetActiveDisplay(DisplayDescriptor display, bool fullscreen)
    {
        _ = fullscreen;
        _activeDisplay = display;
        ApplyCurrentPresentation();
    }

    private async Task CaptureAsync(bool activateAnnotationAfterCapture = true)
    {
        if (_paths is null || _isCapturing)
        {
            return;
        }

        StoreCurrentCrop();
        var previousCapture = _session.CurrentCapture;
        var display = _activeDisplay ?? GetDisplayContainingWindow();
        var number = _session.Captures.Count + 1;
        var screenshotPath = Path.Combine(_paths.WorkingCapturesFolder, $"{number:00}-screen.png");
        var thumbnailPath = Path.Combine(_paths.WorkingCapturesFolder, $"{number:00}-thumb.png");
        var attemptPaths = new HashSet<string>(StringComparer.Ordinal)
        {
            screenshotPath,
            thumbnailPath
        };
        var previousUi = new CaptureUiState(
            _session.Mode,
            IsDisplayDropdownOpen,
            IsCaptureDropdownOpen,
            IsAboutPanelOpen,
            CommentEditor.IsVisible,
            CropOverlay.IsVisible);
        var captureCommitted = false;
        CaptureViewModel? addedCapture = null;
        CaptureOutcomeFeedback? captureFeedback = null;
        _captureCancellation = new CancellationTokenSource();
        var cancellationToken = _captureCancellation.Token;

        try
        {
            CloseCaptureStatusFeedback();
            StartCaptureHeartbeat();
            _isCapturing = true;
            _session.Mode = AnnotationInteractionMode.Capturing;
            IsDisplayDropdownOpen = false;
            IsCaptureDropdownOpen = false;
            IsAboutPanelOpen = false;
            CommentEditor.IsVisible = false;
            CropOverlay.IsVisible = false;
            RefreshCropMaskVisibility(forceHidden: true);
            _windowCoordinator.CloseToolbarPopups();
            SetActiveDisplay(display, fullscreen: true);

            var platformHandle = TryGetPlatformHandle();
            await Dispatcher.UIThread.InvokeAsync(
                static () => { },
                DispatcherPriority.Render);
            _windowIntegration.FlushCompositor();
            await Task.Delay(TimeSpan.FromMilliseconds(180), cancellationToken);
            display = ResolveAvailableDisplay(display);
            _activeDisplay = display;

            var result = await _captureService.CaptureScreenAsync(new ScreenCaptureRequest(
                screenshotPath,
                display,
                IncludeCursor: false,
                CancellationToken: cancellationToken,
                ParentWindow: platformHandle is null ||
                    string.IsNullOrWhiteSpace(platformHandle.HandleDescriptor)
                    ? null
                    : new NativeWindowReference(
                        platformHandle.Handle,
                        platformHandle.HandleDescriptor)));
            if (!result.IsCompleted || result.CapturedScreen is not { } captured)
            {
                captureFeedback = CaptureOutcomeFeedbackPolicy.Create(result.Outcome);
                Debug.WriteLine($"Screen capture ended with {result.Outcome}: {result.ErrorMessage}");
                return;
            }

            attemptPaths.Add(captured.ScreenshotPath);
            File.Copy(captured.ScreenshotPath, thumbnailPath, overwrite: true);

            var capture = new CaptureViewModel(
                Guid.NewGuid().ToString("N"),
                number,
                captured.Display,
                captured.ScreenshotPath,
                thumbnailPath,
                captured.PixelSize,
                captured.Display.Bounds,
                isSelected: true,
                exportScalePercent: _defaultScalePercent);
            ApplyCaptureScale(capture, _defaultScalePercent);
            CaptureCropInheritancePolicy.TryCopyCrop(previousCapture, capture);

            foreach (var existing in _session.Captures)
            {
                existing.IsSelected = false;
            }

            _session.Captures.Add(capture);
            addedCapture = capture;
            SelectCapture(capture);
            if (activateAnnotationAfterCapture)
            {
                SetAnnotationMode(true);
            }
            else
            {
                _session.Mode = AnnotationInteractionMode.Idle;
            }

            captureCommitted = true;
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("Screen capture was cancelled.");
        }
        catch (Exception exception)
        {
            captureFeedback = CaptureOutcomeFeedbackPolicy.Create(ScreenCaptureOutcome.Failed);
            Debug.WriteLine($"Screen capture failed: {exception}");
        }
        finally
        {
            _isCapturing = false;
            _captureCancellation.Dispose();
            _captureCancellation = null;

            if (!captureCommitted)
            {
                if (addedCapture is not null)
                {
                    _session.Captures.Remove(addedCapture);
                    if (previousCapture is not null)
                    {
                        SelectCapture(previousCapture);
                    }
                }

                foreach (var path in attemptPaths)
                {
                    TryDeleteFile(path);
                }

                RestoreCaptureUi(previousUi);
            }

            if (!_hasTerminalStatus)
            {
                StopCaptureHeartbeat();
                ApplyCurrentWindowMode();
                RefreshCropMaskVisibility();
                UpdateChrome();
                ResetIdleTimer();
                if (captureFeedback is not null)
                {
                    ShowCaptureStatusFeedback(captureFeedback);
                }
            }
        }
    }

    private void ShowCaptureStatusFeedback(CaptureOutcomeFeedback feedback)
    {
        IsDisplayDropdownOpen = false;
        IsCaptureDropdownOpen = false;
        IsAboutPanelOpen = false;
        CaptureStatusTitleText.Text = feedback.Title;
        CaptureStatusMessageText.Text = feedback.Message;
        _toolbarWindow.IsCaptureStatusOpen = true;
    }

    private void CloseCaptureStatusFeedback()
    {
        _toolbarWindow.IsCaptureStatusOpen = false;
    }

    private void StartCaptureHeartbeat()
    {
        StopIdleTimers();
        _ = TouchActivityAsync();
        _captureHeartbeatTimer ??= new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _captureHeartbeatTimer.Stop();
        _captureHeartbeatTimer.Tick -= OnCaptureHeartbeat;
        _captureHeartbeatTimer.Tick += OnCaptureHeartbeat;
        _captureHeartbeatTimer.Start();
    }

    private void OnCaptureHeartbeat(object? sender, EventArgs e)
    {
        _ = TouchActivityAsync();
    }

    private void StopCaptureHeartbeat()
    {
        _captureHeartbeatTimer?.Stop();
    }

    private void RestoreCaptureUi(CaptureUiState state)
    {
        _session.Mode = state.Mode;
        IsDisplayDropdownOpen = state.DisplayDropdownVisible;
        IsCaptureDropdownOpen = state.CaptureDropdownVisible;
        IsAboutPanelOpen = state.AboutPanelVisible;
        CommentEditor.IsVisible = state.CommentEditorVisible;
        CropOverlay.IsVisible = state.CropOverlayVisible;
    }

    private async Task RequestCaptureAsync()
    {
        if (!CanUseCaptureControls())
        {
            IsCaptureDropdownOpen = false;
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

    private async Task ActivatePrivacyMaskAsync()
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
            SetPrivacyMaskMode(true);
            return;
        }

        TogglePrivacyMaskMode();
    }

    private void ToggleAnnotationMode()
    {
        SetAnnotationMode(!_isAnnotationToggleActive);
    }

    private void SetAnnotationMode(bool isActive)
    {
        StoreCurrentCrop();
        _isAnnotationToggleActive = isActive && _session.CurrentCapture is not null;
        if (_isAnnotationToggleActive)
        {
            _isPrivacyMaskToggleActive = false;
            CommandBar.SetPrivacyMaskActive(false);
        }

        CommandBar.SetAnnotationActive(_isAnnotationToggleActive);

        if (!_isAnnotationToggleActive)
        {
            _isDrawing = false;
            SetChromeVisible(true);
            _session.Mode = AnnotationInteractionMode.Idle;
            CommentEditor.IsVisible = false;
            CropOverlay.IsVisible = false;
            IsAboutPanelOpen = false;
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

        IsDisplayDropdownOpen = false;
        IsCaptureDropdownOpen = false;
        IsAboutPanelOpen = false;
        CommentEditor.IsVisible = false;
        CropOverlay.IsVisible = false;
        RefreshCropMaskVisibility();
        UpdateChrome();
    }

    private void TogglePrivacyMaskMode()
    {
        SetPrivacyMaskMode(!_isPrivacyMaskToggleActive);
    }

    private void SetPrivacyMaskMode(bool isActive)
    {
        StoreCurrentCrop();
        _isPrivacyMaskToggleActive = isActive && _session.CurrentCapture is not null;
        if (_isPrivacyMaskToggleActive)
        {
            _isAnnotationToggleActive = false;
            CommandBar.SetAnnotationActive(false);
        }

        CommandBar.SetPrivacyMaskActive(_isPrivacyMaskToggleActive);

        if (!_isPrivacyMaskToggleActive)
        {
            _isDrawing = false;
            SetChromeVisible(true);
            _session.Mode = AnnotationInteractionMode.Idle;
            CommentEditor.IsVisible = false;
            CropOverlay.IsVisible = false;
            IsAboutPanelOpen = false;
            RefreshCropMaskVisibility();
            ApplyCurrentWindowMode();
            UpdateChrome();
            return;
        }

        _session.Mode = AnnotationInteractionMode.DrawingPrivacyMask;
        if (_activeDisplay is { } display)
        {
            SetActiveDisplay(display, fullscreen: true);
        }

        IsDisplayDropdownOpen = false;
        IsCaptureDropdownOpen = false;
        IsAboutPanelOpen = false;
        CommentEditor.IsVisible = false;
        CropOverlay.IsVisible = false;
        RefreshCropMaskVisibility();
        UpdateChrome();
    }

    private void SetCurrentCaptureScalePercent(int percent)
    {
        if (_session.CurrentCapture is { } capture)
        {
            ApplyCaptureScale(capture, percent);
        }
    }

    private void ApplyCaptureScale(CaptureViewModel capture, int percent)
    {
        if (_paths is null)
        {
            return;
        }

        var resolvedPercent = ExportScalePercentParser.Clamp(percent);
        var previousPreviewPath = capture.PreviewPath;
        var previewPath = Path.Combine(_paths.WorkingCapturesFolder, $"{capture.Number:00}-preview.png");
        var preview = _previewWriter.Write(capture.ScreenshotPath, previewPath, resolvedPercent);
        capture.SetScalePreview(
            resolvedPercent,
            resolvedPercent == CaptureScale.MaximumPercent ? null : preview.ImagePath,
            preview.PixelSize);

        if (_session.CurrentCapture == capture)
        {
            ScreenshotSurface.SetImage(capture.DisplayImagePath);
            BlurredCropMask.SetImage(capture.DisplayImagePath);
            CaptureScaleSelector.SetCapture(capture.Number, capture.ExportScalePercent, capture.PreviewPixelSize);
            RefreshAnnotations();
            RefreshCropMaskVisibility();
        }

        if (resolvedPercent == CaptureScale.MaximumPercent && previousPreviewPath is not null)
        {
            TryDeleteFile(previousPreviewPath);
        }
    }

    private DisplayDescriptor GetDisplayContainingWindow()
    {
        var scaling = RenderScaling > 0 ? RenderScaling : 1;
        var center = new PointInt(
            Position.X + Math.Max(1, (int)Math.Round(Bounds.Width * scaling / 2)),
            Position.Y + Math.Max(1, (int)Math.Round(Bounds.Height * scaling / 2)));
        return _displayCatalog.GetDisplayContainingPoint(center);
    }

    private DisplayDescriptor ResolveAvailableDisplay(DisplayDescriptor preferred)
    {
        var displays = _displayCatalog.GetDisplays();
        return displays.FirstOrDefault(display =>
                   !string.IsNullOrWhiteSpace(preferred.Id)
                   && string.Equals(display.Id, preferred.Id, StringComparison.Ordinal))
            ?? displays.FirstOrDefault(display =>
                string.Equals(display.Name, preferred.Name, StringComparison.OrdinalIgnoreCase)
                && display.Bounds == preferred.Bounds)
            ?? _displayCatalog.GetDisplayContainingPoint(new PointInt(
                preferred.Bounds.X + preferred.Bounds.Width / 2,
                preferred.Bounds.Y + preferred.Bounds.Height / 2));
    }

    private bool IsBlockingSurfaceActive()
    {
        return CreateCurrentPresentation().OverlayVisible;
    }

    private bool ShouldRenderCaptureSurface()
    {
        return _session.CurrentCapture is not null &&
            CreateCurrentPresentation().CaptureSurfaceVisible;
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
        ApplyCurrentPresentation();
    }

    private OverlayPresentation CreateCurrentPresentation()
    {
        return OverlayPresentationPolicy.Create(
            _isCapturing,
            _isDrawing,
            _session.Mode,
            CropOverlay.IsVisible,
            CommentEditor.IsVisible,
            IdleWarningOverlay.IsVisible,
            SessionConfirmationOverlay.IsVisible,
            _hasTerminalStatus);
    }

    private void ApplyCurrentPresentation()
    {
        var presentation = CreateCurrentPresentation();
        ScreenshotSurface.IsVisible = _session.CurrentCapture is not null && presentation.CaptureSurfaceVisible;
        AnnotationCanvas.IsVisible = ScreenshotSurface.IsVisible;
        _windowCoordinator.Apply(presentation, _activeDisplay);
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
        ScreenshotSurface.SetImage(capture.DisplayImagePath);
        BlurredCropMask.SetImage(capture.DisplayImagePath);
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
        IsCaptureDropdownOpen = false;
        IsDisplayDropdownOpen = false;
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
        _isPrivacyMaskToggleActive = false;
        _session.Mode = AnnotationInteractionMode.Idle;
        CommandBar.SetAnnotationActive(false);
        CommandBar.SetPrivacyMaskActive(false);
        ScreenshotSurface.SetImage(null);
        CaptureScaleSelector.ClearCapture();
        BlurredCropMask.SetImage(null);
        BlurredCropMask.IsVisible = false;
        CommentEditor.IsVisible = false;
        IsAboutPanelOpen = false;
        CropOverlay.IsVisible = false;
        AnnotationCanvas.Children.Clear();
        UpdateChrome();
        ApplyCurrentWindowMode();
    }

    private static void TryDeleteCaptureFiles(CaptureViewModel capture)
    {
        TryDeleteFile(capture.ScreenshotPath);
        TryDeleteFile(capture.ThumbnailPath);
        if (capture.PreviewPath is { } previewPath)
        {
            TryDeleteFile(previewPath);
        }
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
            IsCaptureDropdownOpen = false;
            UpdateChrome();
            return;
        }

        IsCaptureDropdownOpen = !IsCaptureDropdownOpen;
        _session.Mode = IsCaptureDropdownOpen
            ? AnnotationInteractionMode.CaptureDropdownOpen
            : AnnotationInteractionMode.Idle;
        IsDisplayDropdownOpen = false;
        IsAboutPanelOpen = false;
        UpdateChrome();
        ApplyCurrentWindowMode();
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
            _isPrivacyMaskToggleActive = false;
            CommandBar.SetAnnotationActive(false);
            CommandBar.SetPrivacyMaskActive(false);
            _session.Mode = AnnotationInteractionMode.EditingCrop;
            IsAboutPanelOpen = false;
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
            AnnotationInteractionMode.DrawingPrivacyMask or
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

    private void OnAnnotationPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_session.CurrentCapture is null ||
            _session.Mode is not (AnnotationInteractionMode.DrawingAnnotation or AnnotationInteractionMode.DrawingPrivacyMask) ||
            !e.GetCurrentPoint(AnnotationCanvas).Properties.IsLeftButtonPressed)
        {
            return;
        }

        BeginBoxDrawing(e);
    }

    private void OnAnnotationPointerPressedTunnel(object? sender, PointerPressedEventArgs e)
    {
        var isLeftButtonPressed = e.GetCurrentPoint(AnnotationCanvas).Properties.IsLeftButtonPressed;
        if (_session.CurrentCapture is null ||
            !OverlayCreationGesturePolicy.ShouldForceNewBox(
                _session.Mode,
                e.KeyModifiers,
                isLeftButtonPressed))
        {
            return;
        }

        BeginBoxDrawing(e);
        e.Handled = true;
    }

    private void BeginBoxDrawing(PointerPressedEventArgs e)
    {
        _isDrawing = true;
        SetChromeVisible(false);
        _drawStart = e.GetPosition(AnnotationCanvas);
        _draftBox = CreateDraftBox();
        _draftBox.ZIndex = 200;
        Canvas.SetLeft(_draftBox, _drawStart.X);
        Canvas.SetTop(_draftBox, _drawStart.Y);
        AnnotationCanvas.Children.Add(_draftBox);
        _draftWarning = CreateDraftWarning();
        _draftWarning.ZIndex = 210;
        AnnotationCanvas.Children.Add(_draftWarning);
        PositionDraftWarning(new RectInt((int)Math.Round(_drawStart.X), (int)Math.Round(_drawStart.Y), 0, 0));
        e.Pointer.Capture(AnnotationCanvas);
    }

    private Border CreateDraftBox()
    {
        if (_session.Mode == AnnotationInteractionMode.DrawingPrivacyMask)
        {
            return new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(230, 0, 0, 0)),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(2),
                Child = new TextBlock
                {
                    Text = "Privacy mask",
                    Foreground = Brushes.White,
                    FontSize = 12,
                    FontWeight = FontWeight.SemiBold,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                }
            };
        }

        return new Border
        {
            BorderBrush = App.Current?.FindResource("AnnotationStrokeBrush") as Avalonia.Media.IBrush,
            Background = App.Current?.FindResource("AnnotationBrush") as Avalonia.Media.IBrush,
            BorderThickness = new Thickness(2)
        };
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

        if (_session.Mode == AnnotationInteractionMode.DrawingPrivacyMask)
        {
            foreach (var existing in _session.CurrentCapture.PrivacyMasks)
            {
                existing.IsSelected = false;
            }

            foreach (var existing in _session.CurrentCapture.Annotations)
            {
                existing.IsSelected = false;
            }

            _session.SelectedAnnotation = null;
            _commentTarget = null;
            CommentEditor.IsVisible = false;

            _session.CurrentCapture.PrivacyMasks.Add(new PrivacyMaskViewModel(
                Guid.NewGuid().ToString("N"),
                rect)
            {
                IsSelected = true,
            });
        }
        else
        {
            var annotation = new AnnotationViewModel(
                Guid.NewGuid().ToString("N"),
                _session.CurrentCapture.GetNextAnnotationNumber(),
                rect,
                string.Empty,
                isPendingComment: true);

            _session.CurrentCapture.Annotations.Add(annotation);
            SelectAnnotation(annotation);
        }

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
        foreach (var privacyMask in _session.CurrentCapture.PrivacyMasks)
        {
            var mask = new PrivacyMaskBoxControl();
            mask.SetMask(privacyMask);
            mask.Selected += (_, request) => SelectPrivacyMask(request);
            mask.RectChanged += (_, _) => { };
            mask.DeleteRequested += (_, _) =>
            {
                if (_session.CurrentCapture is null)
                {
                    return;
                }

                _session.CurrentCapture.PrivacyMasks.Remove(privacyMask);
                if (privacyMask.IsSelected)
                {
                    privacyMask.IsSelected = false;
                }

                RefreshAnnotations();
            };
            ConfigureOverlayCreationHint(mask);
            Canvas.SetLeft(mask, privacyMask.BoxRect.X);
            Canvas.SetTop(mask, privacyMask.BoxRect.Y);
            AnnotationCanvas.Children.Add(mask);
        }

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
            ConfigureOverlayCreationHint(box);
            Canvas.SetLeft(box, annotation.BoxRect.X);
            Canvas.SetTop(box, annotation.BoxRect.Y);
            AnnotationCanvas.Children.Add(box);
        }

        UpdateOverlayCreationHints();
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

        foreach (var privacyMask in _session.CurrentCapture.PrivacyMasks)
        {
            privacyMask.IsSelected = false;
        }

        _session.SelectedAnnotation = annotation;
        _session.Mode = AnnotationInteractionMode.AnnotationSelected;
        _isPrivacyMaskToggleActive = false;
        CommandBar.SetPrivacyMaskActive(false);
        _commentTarget = annotation;
        CommentEditor.IsVisible = true;
        CommentEditor.Open(annotation.Comment);
        PositionCommentEditor(annotation);
        RefreshCropMaskVisibility();
        UpdateChrome();
        Activate();
        CommentEditor.FocusTextBox();
    }

    private void SelectAnnotation(AnnotationSelectionRequest request)
    {
        if (_session.CurrentCapture is null)
        {
            return;
        }

        SelectOverlay(SelectOverlayAtPoint(request.Point, request.Annotation, request.AllowCycle));
    }

    private void SelectPrivacyMask(PrivacyMaskSelectionRequest request)
    {
        if (_session.CurrentCapture is null)
        {
            return;
        }

        SelectOverlay(SelectOverlayAtPoint(request.Point, request.Mask, request.AllowCycle));
    }

    private object SelectOverlayAtPoint(PointInt point, object requested, bool allowCycle)
    {
        if (!allowCycle || _session.CurrentCapture is null)
        {
            return requested;
        }

        var hits = new List<object>();
        hits.AddRange(_session.CurrentCapture.Annotations
            .OrderByDescending(annotation => annotation.Number)
            .Where(annotation => Contains(annotation.BoxRect, point)));
        for (var index = _session.CurrentCapture.PrivacyMasks.Count - 1; index >= 0; index--)
        {
            var mask = _session.CurrentCapture.PrivacyMasks[index];
            if (Contains(mask.BoxRect, point))
            {
                hits.Add(mask);
            }
        }

        if (hits.Count == 0)
        {
            return requested;
        }

        var current = GetSelectedOverlay();
        var selectedIndex = current is null ? -1 : hits.IndexOf(current);
        return selectedIndex >= 0
            ? hits[(selectedIndex + 1) % hits.Count]
            : hits[0];
    }

    private object? GetSelectedOverlay()
    {
        if (_session.SelectedAnnotation is not null)
        {
            return _session.SelectedAnnotation;
        }

        return _session.CurrentCapture?.PrivacyMasks.FirstOrDefault(mask => mask.IsSelected);
    }

    private void SelectOverlay(object selected)
    {
        switch (selected)
        {
            case AnnotationViewModel annotation:
                SelectAnnotation(annotation);
                break;
            case PrivacyMaskViewModel mask:
                SelectPrivacyMask(mask);
                break;
        }
    }

    private void SelectPrivacyMask(PrivacyMaskViewModel mask)
    {
        if (_session.CurrentCapture is null)
        {
            return;
        }

        foreach (var existing in _session.CurrentCapture.PrivacyMasks)
        {
            existing.IsSelected = existing == mask;
        }

        foreach (var annotation in _session.CurrentCapture.Annotations)
        {
            annotation.IsSelected = false;
        }

        _session.SelectedAnnotation = null;
        _commentTarget = null;
        CommentEditor.IsVisible = false;
        RefreshCropMaskVisibility();
        UpdateChrome();
    }

    private static bool Contains(RectInt rect, PointInt point)
    {
        return point.X >= rect.X &&
            point.X < rect.Right &&
            point.Y >= rect.Y &&
            point.Y < rect.Bottom;
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
        if (_paths is null || _isFinishing)
        {
            return;
        }

        _isFinishing = true;
        try
        {
            if (!await ConfirmSessionActionAsync(SessionConfirmationPolicy.CreateFinish(_launchCaller)))
            {
                return;
            }

            StoreCurrentCrop();
            StoreActiveComment();
            var completionPaths = await ResolveCompletionPathsAsync();
            if (completionPaths is null)
            {
                ResetIdleTimer();
                ApplyCurrentWindowMode();
                return;
            }

            _paths = completionPaths;
            StopIdleTimers();
            var session = BuildExportSession();
            await _exporter.ExportAsync(_paths, session);
            DeleteRawCaptureSourceFiles();
            await _store.MarkCompletedAsync(_paths, _paths.ReviewMarkdownPath, _paths.AnnotationsJsonPath);
            _hasTerminalStatus = true;
            Close();
        }
        finally
        {
            if (!_hasTerminalStatus)
            {
                _isFinishing = false;
            }
        }
    }

    private async Task<bool> ConfirmSessionActionAsync(SessionConfirmationPresentation presentation)
    {
        var wasIdleWarningVisible = IdleWarningOverlay.IsVisible;
        _idleTimer?.Stop();
        _idleWarningTimer?.Stop();
        SessionConfirmationTitleText.Text = presentation.Title;
        SessionConfirmationMessageText.Text = presentation.Message;
        SessionConfirmationConfirmButton.Content = presentation.ConfirmText;
        SetSessionConfirmationActionStyle(presentation.IsDestructive);
        SessionConfirmationOverlay.IsVisible = true;
        if (_activeDisplay is { } display)
        {
            SetActiveDisplay(display, fullscreen: true);
        }
        else
        {
            ApplyCurrentWindowMode();
        }

        SessionConfirmationOverlay.Focus();

        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _sessionConfirmation = completion;
        var confirmed = await completion.Task;
        _sessionConfirmation = null;
        SessionConfirmationOverlay.IsVisible = false;
        if (confirmed)
        {
            IdleWarningOverlay.IsVisible = false;
            _idleWarningTimer?.Stop();
            return true;
        }

        if (wasIdleWarningVisible)
        {
            ContinueAfterIdleWarning();
        }
        else
        {
            ResetIdleTimer();
            ApplyCurrentWindowMode();
        }

        return false;
    }

    private void SetSessionConfirmationActionStyle(bool isDestructive)
    {
        SessionConfirmationConfirmButton.Classes.Remove("confirmButton");
        SessionConfirmationConfirmButton.Classes.Remove("destructiveButton");
        SessionConfirmationConfirmButton.Classes.Add(isDestructive ? "destructiveButton" : "confirmButton");
    }

    private void CompleteSessionConfirmation(bool confirmed)
    {
        _sessionConfirmation?.TrySetResult(confirmed);
    }

    private void OnSessionConfirmationKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CompleteSessionConfirmation(confirmed: false);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            CompleteSessionConfirmation(confirmed: true);
            e.Handled = true;
        }
    }

    private async Task<SessionPaths?> ResolveCompletionPathsAsync()
    {
        if (_paths is null || !CompletionBehavior.RequiresExportDestination(_launchCaller))
        {
            return _paths;
        }

        var exportFolder = await _exportDestinationPicker.PickAsync(this);
        if (string.IsNullOrWhiteSpace(exportFolder))
        {
            return null;
        }

        try
        {
            Directory.CreateDirectory(exportFolder);
            Directory.CreateDirectory(Path.Combine(exportFolder, "captures"));
            return SessionPaths.FromFolder(_paths.SessionFolder, exportFolder);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private void DeleteRawCaptureSourceFiles()
    {
        CaptureSourceCleaner.DeleteRawSources(_session.Captures);
    }

    private async Task CancelAsync()
    {
        _captureCancellation?.Cancel();
        StopIdleTimers();
        if (_paths is not null)
        {
            await _store.MarkCancelledAsync(_paths);
        }

        _hasTerminalStatus = true;
        Close();
    }

    private async Task RequestCancelAsync()
    {
        if (_paths is null || _hasTerminalStatus || _sessionConfirmation is not null)
        {
            return;
        }

        if (await ConfirmSessionActionAsync(SessionConfirmationPolicy.CreateCancel()))
        {
            await CancelAsync();
        }
    }

    private async void OnToolbarClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_hasTerminalStatus)
        {
            return;
        }

        e.Cancel = true;
        await RequestCancelAsync();
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_isFinalClose)
        {
            return;
        }

        if (!_hasTerminalStatus)
        {
            e.Cancel = true;
            await RequestCancelAsync();
            return;
        }

        e.Cancel = true;
        _captureCancellation?.Cancel();
        StopIdleTimers();
        await _toolbarPlacementController.FlushAsync();
        _toolbarPlacementController.Dispose();
        _isFinalClose = true;
        _windowCoordinator.CloseToolbar();
        Close();
    }

    private void StopIdleTimers()
    {
        _idleTimer?.Stop();
        _idleWarningTimer?.Stop();
        StopCaptureHeartbeat();
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
                .ToList(),
            PrivacyMasks: capture.PrivacyMasks
                .Select(mask => new PrivacyMask(mask.MaskId, ToPixelRect(mask.BoxRect, capture)))
                .ToList(),
            ExportScalePercent: capture.ExportScalePercent);
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
        var cropPath = Path.Combine(_paths.WorkingCapturesFolder, $"{capture.Number:00}-crop.png");
        return _cropWriter.WriteCrop(capture.ScreenshotPath, cropPath, crop);
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
        UpdateOverlayCreationHints();
        var canUseCaptureControls = CanUseCaptureControls();
        CommandBar.SetCaptureNumber(_session.CurrentCapture?.Number ?? 0);
        if (_session.CurrentCapture is { } capture)
        {
            CaptureScaleSelector.SetCapture(capture.Number, capture.ExportScalePercent, capture.PreviewPixelSize);
        }
        else
        {
            CaptureScaleSelector.ClearCapture();
        }
        CommandBar.SetCaptureControlsEnabled(canUseCaptureControls);
        CaptureDropdown.SetCaptures(_session.Captures);
        CaptureDropdown.SetCanCreateCapture(canUseCaptureControls);
        DisplayDropdown.SetDisplays(CreateDisplayViewModels());
        if (IsCaptureDropdownOpen)
        {
            Dispatcher.UIThread.Post(ApplyCurrentWindowMode);
        }
    }

    private void UpdateOverlayCreationHints()
    {
        var hint = OverlayCreationGesturePolicy.GetExistingBoxHint(_session.Mode);
        if (hint is null)
        {
            OverlayCreationHint.IsVisible = false;
            return;
        }

        OverlayCreationHintText.Text = hint;
    }

    private void ConfigureOverlayCreationHint(Control control)
    {
        control.PointerEntered += OnOverlayBoxPointerEntered;
        control.PointerMoved += OnOverlayBoxPointerMoved;
        control.PointerExited += OnOverlayBoxPointerExited;
    }

    private void OnOverlayBoxPointerEntered(object? sender, PointerEventArgs e)
    {
        MoveOverlayCreationHint(e);
    }

    private void OnOverlayBoxPointerMoved(object? sender, PointerEventArgs e)
    {
        MoveOverlayCreationHint(e);
    }

    private void OnOverlayBoxPointerExited(object? sender, PointerEventArgs e)
    {
        OverlayCreationHint.IsVisible = false;
    }

    private void MoveOverlayCreationHint(PointerEventArgs e)
    {
        var hint = OverlayCreationGesturePolicy.GetExistingBoxHint(_session.Mode);
        if (hint is null)
        {
            OverlayCreationHint.IsVisible = false;
            return;
        }

        OverlayCreationHintText.Text = hint;
        OverlayCreationHint.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var hintSize = OverlayCreationHint.DesiredSize;
        var viewport = ChromeCanvas.Bounds.Size;
        var pointer = e.GetPosition(ChromeCanvas);
        const double offset = 14;
        var x = pointer.X + offset;
        var y = pointer.Y + offset;

        if (x + hintSize.Width > viewport.Width)
        {
            x = pointer.X - hintSize.Width - offset;
        }

        if (y + hintSize.Height > viewport.Height)
        {
            y = pointer.Y - hintSize.Height - offset;
        }

        Canvas.SetLeft(
            OverlayCreationHint,
            Math.Clamp(x, 0, Math.Max(0, viewport.Width - hintSize.Width)));
        Canvas.SetTop(
            OverlayCreationHint,
            Math.Clamp(y, 0, Math.Max(0, viewport.Height - hintSize.Height)));
        OverlayCreationHint.IsVisible = true;
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
                string.Equals(display.Id, current.Id, StringComparison.Ordinal)))
            .ToList();
    }

    private static double Read(double value)
    {
        return double.IsNaN(value) ? 0 : value;
    }

    private void SetChromeVisible(bool isVisible)
    {
        if (!isVisible)
        {
            OverlayCreationHint.IsVisible = false;
        }

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

        if (_session.Mode == AnnotationInteractionMode.DrawingPrivacyMask)
        {
            _draftBox.BorderBrush = AnnotationRectPolicy.IsMinimumSizeReached(rect, MinimumAnnotationSize)
                ? Brushes.White
                : App.Current?.FindResource("InvalidAnnotationStrokeBrush") as Avalonia.Media.IBrush;
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

    private sealed record CaptureUiState(
        AnnotationInteractionMode Mode,
        bool DisplayDropdownVisible,
        bool CaptureDropdownVisible,
        bool AboutPanelVisible,
        bool CommentEditorVisible,
        bool CropOverlayVisible);

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
