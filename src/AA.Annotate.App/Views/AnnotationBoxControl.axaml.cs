using AA.Annotate.App.ViewModels;
using AA.Annotate.Core.Geometry;
using AA.Annotate.Core.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System.ComponentModel;

namespace AA.Annotate.App.Views;

public partial class AnnotationBoxControl : UserControl
{
    private const double RestOpacity = 0.46;
    private const double ActiveOpacity = 1;
    private const int RestZIndex = 20;
    private const int SelectedZIndex = 100;
    private bool _isDragging;
    private bool _isResizing;
    private bool _isPointerOver;
    private RectResizeHandle _resizeHandle;
    private Point _dragStart;
    private Rect _origin;

    public event EventHandler<AnnotationSelectionRequest>? Selected;

    public event EventHandler<RectInt>? RectChanged;

    public AnnotationBoxControl()
    {
        InitializeComponent();
        PointerPressed += OnPointerPressed;
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
        LeftResizeHandle.PointerPressed += (_, e) => BeginResize(RectResizeHandle.Left, e);
        TopResizeHandle.PointerPressed += (_, e) => BeginResize(RectResizeHandle.Top, e);
        RightResizeHandle.PointerPressed += (_, e) => BeginResize(RectResizeHandle.Right, e);
        BottomResizeHandle.PointerPressed += (_, e) => BeginResize(RectResizeHandle.Bottom, e);
        TopLeftResizeHandle.PointerPressed += (_, e) => BeginResize(RectResizeHandle.TopLeft, e);
        TopRightResizeHandle.PointerPressed += (_, e) => BeginResize(RectResizeHandle.TopRight, e);
        BottomRightResizeHandle.PointerPressed += (_, e) => BeginResize(RectResizeHandle.BottomRight, e);
        BottomLeftResizeHandle.PointerPressed += (_, e) => BeginResize(RectResizeHandle.BottomLeft, e);
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
        UpdateExportIndicator();
        UpdateVisualState();
    }

    private void ApplyRect(RectInt rect)
    {
        Width = rect.Width;
        Height = rect.Height;
        BoxBorder.Width = rect.Width;
        BoxBorder.Height = rect.Height;
        Canvas.SetLeft(ExportIndicator, Math.Max(0, rect.Width - 10));
        Canvas.SetTop(ExportIndicator, -8);
        UpdateHandleLayout(rect);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (Annotation is null)
        {
            return;
        }

        Selected?.Invoke(this, new AnnotationSelectionRequest(Annotation, ToParentPoint(e), AllowCycle: true));
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

    private void BeginResize(RectResizeHandle handle, PointerPressedEventArgs e)
    {
        if (Annotation is null)
        {
            return;
        }

        Selected?.Invoke(this, new AnnotationSelectionRequest(Annotation, ToParentPoint(e), AllowCycle: false));
        _isResizing = true;
        _resizeHandle = handle;
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
            next = RectResizer.Resize(
                ToRectInt(_origin),
                _resizeHandle,
                new PointInt((int)Math.Round(delta.X), (int)Math.Round(delta.Y)),
                GetParentBounds(parent),
                AnnotationRectPolicy.MinimumSize);
            Canvas.SetLeft(this, next.X);
            Canvas.SetTop(this, next.Y);
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
        else if (e.PropertyName == nameof(AnnotationViewModel.ExportState))
        {
            UpdateExportIndicator();
            UpdateVisualState();
        }
    }

    private void UpdateVisualState()
    {
        var active = Annotation?.IsSelected == true || _isPointerOver || _isDragging || _isResizing;
        var opacity = active ? ActiveOpacity : RestOpacity;
        var showHandles = Annotation?.IsSelected == true || _isDragging || _isResizing;
        BoxBorder.Opacity = opacity;
        BoxBorder.BorderThickness = active ? new Thickness(2) : new Thickness(1.5);
        NumberBadge.Opacity = active ? ActiveOpacity : 0.55;
        ExportIndicator.Opacity = active ? ActiveOpacity : 0.72;
        ZIndex = showHandles ? SelectedZIndex : RestZIndex;
        foreach (var handle in GetResizeHandles())
        {
            handle.IsVisible = showHandles;
            handle.Opacity = active ? ActiveOpacity : 0.72;
        }
    }

    private void UpdateExportIndicator()
    {
        if (Annotation is null || Annotation.ExportState == AnnotationCropExportState.Included)
        {
            ExportIndicator.IsVisible = false;
            ToolTip.SetTip(ExportIndicator, null);
            return;
        }

        ExportIndicator.IsVisible = true;
        if (Annotation.ExportState == AnnotationCropExportState.Clipped)
        {
            ExportIndicatorText.Text = "!";
            ExportIndicator.Background = new SolidColorBrush(Color.Parse("#CCB45309"));
            ToolTip.SetTip(ExportIndicator, "Will be clipped on export");
            return;
        }

        ExportIndicatorText.Text = "X";
        ExportIndicator.Background = new SolidColorBrush(Color.Parse("#CC7F1D1D"));
        ToolTip.SetTip(ExportIndicator, "Will be excluded on export");
    }

    private static double Read(double value)
    {
        return double.IsNaN(value) ? 0 : value;
    }

    private void UpdateHandleLayout(RectInt rect)
    {
        var width = Math.Max(AnnotationRectPolicy.MinimumSize, rect.Width);
        var height = Math.Max(AnnotationRectPolicy.MinimumSize, rect.Height);

        Canvas.SetLeft(LeftResizeHandle, -LeftResizeHandle.Width / 2);
        Canvas.SetTop(LeftResizeHandle, height / 2d - LeftResizeHandle.Height / 2);

        Canvas.SetLeft(TopResizeHandle, width / 2d - TopResizeHandle.Width / 2);
        Canvas.SetTop(TopResizeHandle, -TopResizeHandle.Height / 2);

        Canvas.SetLeft(RightResizeHandle, width - RightResizeHandle.Width / 2);
        Canvas.SetTop(RightResizeHandle, height / 2d - RightResizeHandle.Height / 2);

        Canvas.SetLeft(BottomResizeHandle, width / 2d - BottomResizeHandle.Width / 2);
        Canvas.SetTop(BottomResizeHandle, height - BottomResizeHandle.Height / 2);

        Canvas.SetLeft(TopLeftResizeHandle, -TopLeftResizeHandle.Width / 2);
        Canvas.SetTop(TopLeftResizeHandle, -TopLeftResizeHandle.Height / 2);

        Canvas.SetLeft(TopRightResizeHandle, width - TopRightResizeHandle.Width / 2);
        Canvas.SetTop(TopRightResizeHandle, -TopRightResizeHandle.Height / 2);

        Canvas.SetLeft(BottomRightResizeHandle, width - BottomRightResizeHandle.Width / 2);
        Canvas.SetTop(BottomRightResizeHandle, height - BottomRightResizeHandle.Height / 2);

        Canvas.SetLeft(BottomLeftResizeHandle, -BottomLeftResizeHandle.Width / 2);
        Canvas.SetTop(BottomLeftResizeHandle, height - BottomLeftResizeHandle.Height / 2);
    }

    private IEnumerable<Border> GetResizeHandles()
    {
        yield return LeftResizeHandle;
        yield return TopResizeHandle;
        yield return RightResizeHandle;
        yield return BottomResizeHandle;
        yield return TopLeftResizeHandle;
        yield return TopRightResizeHandle;
        yield return BottomRightResizeHandle;
        yield return BottomLeftResizeHandle;
    }

    private PointInt ToParentPoint(PointerPressedEventArgs e)
    {
        var point = Parent is Visual parent
            ? e.GetPosition(parent)
            : e.GetPosition(this);
        return new PointInt((int)Math.Round(point.X), (int)Math.Round(point.Y));
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

    private static RectInt ToRectInt(Rect rect)
    {
        return new RectInt(
            (int)Math.Round(rect.X),
            (int)Math.Round(rect.Y),
            Math.Max(1, (int)Math.Round(rect.Width)),
            Math.Max(1, (int)Math.Round(rect.Height)));
    }
}
