using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia;

namespace AA.Annotate.App.Views;

public partial class FloatingCommandBar : UserControl
{
    private DispatcherTimer? _attentionTimer;

    public event EventHandler? MoveSelectorRequested;

    public event EventHandler? CaptureRequested;

    public event EventHandler? CaptureSelectorRequested;

    public event EventHandler? CropRequested;

    public event EventHandler? AnnotationRequested;

    public event EventHandler? FinishRequested;

    public event EventHandler? CancelRequested;

    public FloatingCommandBar()
    {
        InitializeComponent();
        MoveButton.Click += (_, _) => MoveSelectorRequested?.Invoke(this, EventArgs.Empty);
        CaptureButton.Click += (_, _) => CaptureRequested?.Invoke(this, EventArgs.Empty);
        CaptureSelectorButton.Click += (_, _) => CaptureSelectorRequested?.Invoke(this, EventArgs.Empty);
        CropButton.Click += (_, _) => CropRequested?.Invoke(this, EventArgs.Empty);
        AnnotationButton.Click += (_, _) => AnnotationRequested?.Invoke(this, EventArgs.Empty);
        FinishButton.Click += (_, _) => FinishRequested?.Invoke(this, EventArgs.Empty);
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

    public void SetCaptureControlsEnabled(bool isEnabled)
    {
        CaptureButton.IsEnabled = isEnabled;
        CaptureSelectorButton.IsEnabled = isEnabled;
        CaptureButton.Opacity = isEnabled ? 1 : 0.45;
        CaptureSelectorButton.Opacity = isEnabled ? 1 : 0.45;
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
                Opacity = 1;
                _attentionTimer?.Stop();
                return;
            }

            var envelope = Math.Sin(Math.PI * progress);
            var pulse = Math.Max(0, Math.Sin(progress * Math.PI * 6));
            var scale = 1 + envelope * (0.025 + pulse * 0.045);
            transform.ScaleX = scale;
            transform.ScaleY = scale;
            Opacity = 0.9 + envelope * 0.1;
        };
        _attentionTimer.Start();
    }
}
