using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia;
using AA.Annotate.App.ViewModels;

namespace AA.Annotate.App.Views;

public partial class FloatingCommandBar : UserControl
{
    private readonly IBrush? _restBrush;
    private readonly IBrush? _solidBrush;
    private DispatcherTimer? _attentionTimer;
    private bool _isPanelHoverActive;
    private bool _isUpdatingScale;
    private int _currentScalePercent = 100;

    public event EventHandler? MoveSelectorRequested;

    public event EventHandler? CaptureRequested;

    public event EventHandler? CaptureSelectorRequested;

    public event EventHandler? CropRequested;

    public event EventHandler? PrivacyMaskRequested;

    public event EventHandler? AnnotationRequested;

    public event EventHandler<int>? ExportScaleChanged;

    public event EventHandler? FinishRequested;

    public event EventHandler? AboutRequested;

    public event EventHandler? CancelRequested;

    public FloatingCommandBar()
    {
        InitializeComponent();
        _restBrush = App.Current?.FindResource("OverlayRestBrush") as IBrush;
        _solidBrush = App.Current?.FindResource("OverlaySolidBrush") as IBrush;
        Opacity = 1;
        RootBorder.Background = _restBrush;
        MoveButton.Click += (_, _) => MoveSelectorRequested?.Invoke(this, EventArgs.Empty);
        CaptureButton.Click += (_, _) => CaptureRequested?.Invoke(this, EventArgs.Empty);
        CaptureSelectorButton.Click += (_, _) => CaptureSelectorRequested?.Invoke(this, EventArgs.Empty);
        CropButton.Click += (_, _) => CropRequested?.Invoke(this, EventArgs.Empty);
        PrivacyMaskButton.Click += (_, _) => PrivacyMaskRequested?.Invoke(this, EventArgs.Empty);
        AnnotationButton.Click += (_, _) => AnnotationRequested?.Invoke(this, EventArgs.Empty);
        ScaleTextBox.LostFocus += (_, _) => CommitScaleText();
        ScaleTextBox.KeyDown += OnScaleTextKeyDown;
        ScalePresetButton.PointerPressed += (_, e) =>
        {
            ScalePresetPopup.IsOpen = !ScalePresetPopup.IsOpen;
            e.Handled = true;
        };
        ScalePreset100Button.Click += (_, _) => SelectScalePreset(100);
        ScalePreset75Button.Click += (_, _) => SelectScalePreset(75);
        ScalePreset66Button.Click += (_, _) => SelectScalePreset(66);
        ScalePreset50Button.Click += (_, _) => SelectScalePreset(50);
        ScalePreset33Button.Click += (_, _) => SelectScalePreset(33);
        ScalePreset25Button.Click += (_, _) => SelectScalePreset(25);
        FinishButton.Click += (_, _) => FinishRequested?.Invoke(this, EventArgs.Empty);
        AboutButton.Click += (_, _) => AboutRequested?.Invoke(this, EventArgs.Empty);
        CancelButton.Click += (_, _) => CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    public void SetCaptureNumber(int number)
    {
        CaptureNumberText.Text = number <= 0 ? string.Empty : number.ToString();
    }

    public void SetAnnotationActive(bool isActive)
    {
        AnnotationButton.Classes.Set("activeIconButton", isActive);
        AnnotationButton.Classes.Set("iconButton", !isActive);
    }

    public void SetPrivacyMaskActive(bool isActive)
    {
        PrivacyMaskButton.Classes.Set("activeIconButton", isActive);
        PrivacyMaskButton.Classes.Set("iconButton", !isActive);
    }

    public void SetExportScalePercent(int percent)
    {
        _currentScalePercent = ExportScalePercentParser.Clamp(percent);
        _isUpdatingScale = true;
        ScaleTextBox.Text = $"{_currentScalePercent}%";
        _isUpdatingScale = false;
    }

    private void OnScaleTextKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        CommitScaleText();
        e.Handled = true;
    }

    private void CommitScaleText()
    {
        if (_isUpdatingScale)
        {
            return;
        }

        var percent = ExportScalePercentParser.ParseOrDefault(ScaleTextBox.Text, _currentScalePercent);
        SetExportScalePercent(percent);
        ExportScaleChanged?.Invoke(this, percent);
    }

    private void SelectScalePreset(int percent)
    {
        SetExportScalePercent(percent);
        ScalePresetPopup.IsOpen = false;
        ExportScaleChanged?.Invoke(this, percent);
    }

    public void SetCaptureControlsEnabled(bool isEnabled)
    {
        CaptureButton.IsEnabled = isEnabled;
        CaptureSelectorButton.IsEnabled = isEnabled;
        CaptureButton.Opacity = isEnabled ? 1 : 0.45;
        CaptureSelectorButton.Opacity = isEnabled ? 1 : 0.45;
    }

    public void SetPanelHoverActive(bool isActive)
    {
        _isPanelHoverActive = isActive;
        ApplyPanelState();
    }

    public void PlayStartupAttentionAnimation()
    {
        _attentionTimer?.Stop();
        var transform = new ScaleTransform(1, 1);
        RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        RenderTransform = transform;

        var startedAt = DateTimeOffset.UtcNow;
        var duration = TimeSpan.FromMilliseconds(1400);
        _attentionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _attentionTimer.Tick += (_, _) =>
        {
            var progress = Math.Clamp((DateTimeOffset.UtcNow - startedAt).TotalMilliseconds / duration.TotalMilliseconds, 0, 1);
            if (progress >= 1)
            {
                transform.ScaleX = 1;
                transform.ScaleY = 1;
                ApplyPanelState();
                _attentionTimer?.Stop();
                return;
            }

            var envelope = Math.Sin(Math.PI * progress);
            var pulse = Math.Max(0, Math.Sin(progress * Math.PI * 6));
            var scale = 1 + envelope * (0.025 + pulse * 0.045);
            transform.ScaleX = scale;
            transform.ScaleY = scale;
            Opacity = 1;
        };
        _attentionTimer.Start();
    }

    private void ApplyPanelState()
    {
        Opacity = 1;
        RootBorder.Background = _isPanelHoverActive ? _solidBrush : _restBrush;
    }
}
