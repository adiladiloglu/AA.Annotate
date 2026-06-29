using AA.Annotate.App.ViewModels;
using AA.Annotate.Core.Geometry;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System.ComponentModel;

namespace AA.Annotate.App.Views;

public partial class AnnotationBoxControl : UserControl
{
    private const double RestOpacity = 0.34;
    private const double ActiveOpacity = 1;
    private const double MinSize = 24;
    private bool _isDragging;
    private bool _isResizing;
    private bool _isPointerOver;
    private Point _dragStart;
    private Rect _origin;

    public event EventHandler<AnnotationViewModel>? Selected;

    public event EventHandler<RectInt>? RectChanged;

    public AnnotationBoxControl()
    {
        InitializeComponent();
        PointerPressed += OnPointerPressed;
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
        ResizeHandle.PointerPressed += OnResizePointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
    }

    public AnnotationViewModel? Annotation { get; private set; }

    public void SetAnnotation(AnnotationViewModel annotation)
    {
        if (Annotation is not null)
        {
            Annotation.PropertyChanged -= OnAnnotationPropertyChanged;
        }

        Annotation = annotation;
        Annotation.PropertyChanged += OnAnnotationPropertyChanged;
        NumberText.Text = annotation.Number.ToString();
        ApplyRect(annotation.BoxRect);
        UpdateVisualState();
    }

    private void ApplyRect(RectInt rect)
    {
        Width = rect.Width;
        Height = rect.Height;
        BoxBorder.Width = rect.Width;
        BoxBorder.Height = rect.Height;
        Canvas.SetLeft(ResizeHandle, Math.Max(0, rect.Width - 6));
        Canvas.SetTop(ResizeHandle, Math.Max(0, rect.Height - 6));
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Annotation is null)
        {
            return;
        }

        Selected?.Invoke(this, Annotation);
        _isDragging = true;
        CaptureStart(e);
        e.Handled = true;
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        _isPointerOver = true;
        UpdateVisualState();
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        _isPointerOver = false;
        UpdateVisualState();
    }

    private void OnResizePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Annotation is null)
        {
            return;
        }

        Selected?.Invoke(this, Annotation);
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
        if (Annotation is null || (!_isDragging && !_isResizing) || Parent is not Visual parent)
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
                Math.Max((int)MinSize, (int)Math.Round(_origin.Width + delta.X)),
                Math.Max((int)MinSize, (int)Math.Round(_origin.Height + delta.Y)));
            ApplyRect(next);
        }
        else
        {
            next = new RectInt(
                Math.Max(0, (int)Math.Round(_origin.X + delta.X)),
                Math.Max(0, (int)Math.Round(_origin.Y + delta.Y)),
                Math.Max(1, (int)Math.Round(_origin.Width)),
                Math.Max(1, (int)Math.Round(_origin.Height)));
            Canvas.SetLeft(this, next.X);
            Canvas.SetTop(this, next.Y);
        }

        Annotation.BoxRect = next;
        RectChanged?.Invoke(this, next);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isDragging = false;
        _isResizing = false;
        e.Pointer.Capture(null);
        UpdateVisualState();
    }

    private void OnAnnotationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AnnotationViewModel.IsSelected))
        {
            UpdateVisualState();
        }
    }

    private void UpdateVisualState()
    {
        var active = Annotation?.IsSelected == true || _isPointerOver || _isDragging || _isResizing;
        var opacity = active ? ActiveOpacity : RestOpacity;
        BoxBorder.Opacity = opacity;
        NumberBadge.Opacity = active ? ActiveOpacity : 0.55;
        ResizeHandle.Opacity = active ? ActiveOpacity : 0.18;
    }

    private static double Read(double value)
    {
        return double.IsNaN(value) ? 0 : value;
    }
}
