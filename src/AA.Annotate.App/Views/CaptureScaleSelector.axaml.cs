using AA.Annotate.App.ViewModels;
using AA.Annotate.Core.Geometry;
using Avalonia.Controls;
using Avalonia.Input;

namespace AA.Annotate.App.Views;

public partial class CaptureScaleSelector : UserControl
{
    private bool _isUpdating;
    private int _currentScalePercent = 100;

    public CaptureScaleSelector()
    {
        InitializeComponent();
        ScaleTextBox.LostFocus += (_, _) => CommitScaleText();
        ScaleTextBox.KeyDown += OnScaleTextKeyDown;
        ScalePresetButton.Click += (_, _) =>
        {
            ScalePresetPopup.IsOpen = !ScalePresetPopup.IsOpen;
        };
        ScalePreset100Button.Click += (_, _) => SelectScalePreset(100);
        ScalePreset75Button.Click += (_, _) => SelectScalePreset(75);
        ScalePreset66Button.Click += (_, _) => SelectScalePreset(66);
        ScalePreset50Button.Click += (_, _) => SelectScalePreset(50);
        ScalePreset33Button.Click += (_, _) => SelectScalePreset(33);
        ScalePreset25Button.Click += (_, _) => SelectScalePreset(25);
    }

    public event EventHandler<int>? ScaleChanged;

    public void SetCapture(int captureNumber, int scalePercent, SizeInt pixelSize)
    {
        IsVisible = true;
        CaptureLabel.Text = $"Capture {captureNumber} quality";
        SetScale(scalePercent, pixelSize);
    }

    public void ClearCapture()
    {
        ClosePopup();
        IsVisible = false;
    }

    public void ClosePopup()
    {
        ScalePresetPopup.IsOpen = false;
    }

    public void SetScale(int scalePercent, SizeInt pixelSize)
    {
        _currentScalePercent = ExportScalePercentParser.Clamp(scalePercent);
        _isUpdating = true;
        ScaleTextBox.Text = $"{_currentScalePercent}%";
        PixelSizeText.Text = $"{pixelSize.Width} x {pixelSize.Height} px";
        _isUpdating = false;
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
        if (_isUpdating)
        {
            return;
        }

        var percent = ExportScalePercentParser.ParseOrDefault(ScaleTextBox.Text, _currentScalePercent);
        ScaleChanged?.Invoke(this, percent);
    }

    private void SelectScalePreset(int percent)
    {
        ScalePresetPopup.IsOpen = false;
        ScaleChanged?.Invoke(this, percent);
    }
}
