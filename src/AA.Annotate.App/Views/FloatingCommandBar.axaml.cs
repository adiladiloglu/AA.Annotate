using Avalonia.Controls;

namespace AA.Annotate.App.Views;

public partial class FloatingCommandBar : UserControl
{
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
}
