using System.ComponentModel;
using AA.Annotate.App.ViewModels;
using AA.Annotate.Core.Geometry;
using AA.Annotate.Core.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace AA.Annotate.App.Views;

public partial class PrivacyMaskBoxControl : UserControl
{
    private bool _isDragging;
    private bool _isResizing;
    private Point _dragStart;
    private Rect _origin;

    public event EventHandler<RectInt>? RectChanged;

    public event EventHandler? DeleteRequested;

    public PrivacyMaskBoxControl()
    {
        InitializeComponent();
        PointerPressed += OnPointerPressed;
        ResizeHandle.PointerPressed += OnResizePointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        DeleteButton.Click += (_, _) => DeleteRequested?.Invoke(this, EventArgs.Empty);
    }

    public PrivacyMaskViewModel? Mask { get; private set; }

    public void SetMask(PrivacyMaskViewModel mask)
    {
        if (Mask is not null)
        {
            Mask.PropertyChanged -= OnMaskPropertyChanged;
        }

        Mask = mask;
        Mask.PropertyChanged += OnMaskPropertyChanged;
        ApplyRect(mask.BoxRect);
    }

    private void ApplyRect(RectInt rect)
    {
        Width = rect.Width;
        Height = rect.Height;
        BoxBorder.Width = rect.Width;
        BoxBorder.Height = rect.Height;
        Canvas.SetLeft(DeleteButton, Math.Max(0, rect.Width - 18));
        Canvas.SetTop(DeleteButton, 0);
        Canvas.SetLeft(ResizeHandle, Math.Max(0, rect.Width - 6));
        Canvas.SetTop(ResizeHandle, Math.Max(0, rect.Height - 6));
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Mask is null)
        {
            return;
        }

        _isDragging = true;
        CaptureStart(e);
        e.Handled = true;
    }

    private void OnResizePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Mask is null)
        {
            return;
        }

        _isResizing = true;
        CaptureStart(e);
        e.Handled = true;
    }

    private void CaptureStart(PointerPressedEventArgs e)
    {
        _dragStart = e.GetPosition(Parent as Visual);
        _origin = new Rect(Read(Canvas.GetLeft(this)), Read(Canvas.GetTop(this)), Width, Height);
        e.Pointer.Capture(this);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (Mask is null || (!_isDragging && !_isResizing) || Parent is not Visual parent)
        {
            return;
        }

        var current = e.GetPosition(parent);
        var delta = current - _dragStart;
        RectInt next;
        if (_isResizing)
        {
            next = new RectInt(
                (int)Math.Round(_origin.X),
                (int)Math.Round(_origin.Y),
                Math.Max(AnnotationRectPolicy.MinimumSize, (int)Math.Round(_origin.Width + delta.X)),
                Math.Max(AnnotationRectPolicy.MinimumSize, (int)Math.Round(_origin.Height + delta.Y)));
            next = AnnotationRectPolicy.ClampToBounds(next, GetParentBounds(parent));
            ApplyRect(next);
        }
        else
        {
            next = new RectInt(
                Math.Max(0, (int)Math.Round(_origin.X + delta.X)),
                Math.Max(0, (int)Math.Round(_origin.Y + delta.Y)),
                Math.Max(1, (int)Math.Round(_origin.Width)),
                Math.Max(1, (int)Math.Round(_origin.Height)));
            next = AnnotationRectPolicy.ClampToBounds(next, GetParentBounds(parent));
            Canvas.SetLeft(this, next.X);
            Canvas.SetTop(this, next.Y);
        }

        Mask.BoxRect = next;
        RectChanged?.Invoke(this, next);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
        _isResizing = false;
        e.Pointer.Capture(null);
    }

    private void OnMaskPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PrivacyMaskViewModel.BoxRect) && Mask is not null)
        {
            ApplyRect(Mask.BoxRect);
        }
    }

    private static double Read(double value)
    {
        return double.IsNaN(value) ? 0 : value;
    }

    private static SizeInt GetParentBounds(Visual parent)
    {
        if (parent is not Control control)
        {
            return new SizeInt(1, 1);
        }

        return new SizeInt(
            Math.Max(1, (int)Math.Round(control.Bounds.Width)),
            Math.Max(1, (int)Math.Round(control.Bounds.Height)));
    }
}
