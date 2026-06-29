using AA.Annotate.Core.Geometry;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace AA.Annotate.App.Views;

public partial class CropOverlay : UserControl
{
    private const int MinSize = 48;
    private CropInteraction _interaction = CropInteraction.None;
    private RectResizeHandle _resizeHandle;
    private Point _dragStart;
    private Rect _origin;

    public event EventHandler<RectInt>? CropChanged;

    public CropOverlay()
    {
        InitializeComponent();
        CropRect.PointerPressed += OnRectPointerPressed;
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
        SizeChanged += (_, _) => UpdateHandleLayout();
    }

    public void SetCrop(RectInt rect)
    {
        Canvas.SetLeft(CropRect, rect.X);
        Canvas.SetTop(CropRect, rect.Y);
        CropRect.Width = rect.Width;
        CropRect.Height = rect.Height;
        UpdateHandleLayout();
        RaiseCropChanged();
    }

    public RectInt GetCrop()
    {
        return new RectInt(
            (int)Math.Round(Read(Canvas.GetLeft(CropRect))),
            (int)Math.Round(Read(Canvas.GetTop(CropRect))),
            Math.Max(1, (int)Math.Round(CropRect.Width)),
            Math.Max(1, (int)Math.Round(CropRect.Height)));
    }

    private void OnRectPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _interaction = CropInteraction.Move;
        CaptureStart(e);
    }

    private void BeginResize(RectResizeHandle handle, PointerPressedEventArgs e)
    {
        _interaction = CropInteraction.Resize;
        _resizeHandle = handle;
        CaptureStart(e);
        e.Handled = true;
    }

    private void CaptureStart(PointerPressedEventArgs e)
    {
        _dragStart = e.GetPosition(this);
        _origin = new Rect(Read(Canvas.GetLeft(CropRect)), Read(Canvas.GetTop(CropRect)), CropRect.Width, CropRect.Height);
        e.Pointer.Capture(this);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_interaction == CropInteraction.None)
        {
            return;
        }

        var current = e.GetPosition(this);
        var delta = current - _dragStart;
        switch (_interaction)
        {
            case CropInteraction.Move:
                MoveCrop(delta);
                break;
            case CropInteraction.Resize:
                ResizeCrop(delta);
                break;
        }

        UpdateHandleLayout();
        RaiseCropChanged();
    }

    private void MoveCrop(Vector delta)
    {
        var maxLeft = Math.Max(0, Bounds.Width - _origin.Width);
        var maxTop = Math.Max(0, Bounds.Height - _origin.Height);
        Canvas.SetLeft(CropRect, Math.Clamp(_origin.X + delta.X, 0, maxLeft));
        Canvas.SetTop(CropRect, Math.Clamp(_origin.Y + delta.Y, 0, maxTop));
    }

    private void ResizeCrop(Vector delta)
    {
        var resized = RectResizer.Resize(
            ToRectInt(_origin),
            _resizeHandle,
            new PointInt((int)Math.Round(delta.X), (int)Math.Round(delta.Y)),
            new SizeInt(Math.Max(1, (int)Math.Round(Bounds.Width)), Math.Max(1, (int)Math.Round(Bounds.Height))),
            MinSize);

        ApplyCrop(resized);
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _interaction = CropInteraction.None;
        e.Pointer.Capture(null);
    }

    private void UpdateHandleLayout()
    {
        var left = Read(Canvas.GetLeft(CropRect));
        var top = Read(Canvas.GetTop(CropRect));
        var width = Math.Max(MinSize, CropRect.Width);
        var height = Math.Max(MinSize, CropRect.Height);

        Canvas.SetLeft(LeftResizeHandle, left - LeftResizeHandle.Width / 2);
        Canvas.SetTop(LeftResizeHandle, top + height / 2 - LeftResizeHandle.Height / 2);

        Canvas.SetLeft(TopResizeHandle, left + width / 2 - TopResizeHandle.Width / 2);
        Canvas.SetTop(TopResizeHandle, top - TopResizeHandle.Height / 2);

        Canvas.SetLeft(RightResizeHandle, left + width - RightResizeHandle.Width / 2);
        Canvas.SetTop(RightResizeHandle, top + height / 2 - RightResizeHandle.Height / 2);

        Canvas.SetLeft(BottomResizeHandle, left + width / 2 - BottomResizeHandle.Width / 2);
        Canvas.SetTop(BottomResizeHandle, top + height - BottomResizeHandle.Height / 2);

        Canvas.SetLeft(TopLeftResizeHandle, left - TopLeftResizeHandle.Width / 2);
        Canvas.SetTop(TopLeftResizeHandle, top - TopLeftResizeHandle.Height / 2);

        Canvas.SetLeft(TopRightResizeHandle, left + width - TopRightResizeHandle.Width / 2);
        Canvas.SetTop(TopRightResizeHandle, top - TopRightResizeHandle.Height / 2);

        Canvas.SetLeft(BottomRightResizeHandle, left + width - BottomRightResizeHandle.Width / 2);
        Canvas.SetTop(BottomRightResizeHandle, top + height - BottomRightResizeHandle.Height / 2);

        Canvas.SetLeft(BottomLeftResizeHandle, left - BottomLeftResizeHandle.Width / 2);
        Canvas.SetTop(BottomLeftResizeHandle, top + height - BottomLeftResizeHandle.Height / 2);
    }

    private void RaiseCropChanged()
    {
        CropChanged?.Invoke(this, GetCrop());
    }

    private static double Read(double value)
    {
        return double.IsNaN(value) ? 0 : value;
    }

    private void ApplyCrop(RectInt rect)
    {
        Canvas.SetLeft(CropRect, rect.X);
        Canvas.SetTop(CropRect, rect.Y);
        CropRect.Width = rect.Width;
        CropRect.Height = rect.Height;
    }

    private static RectInt ToRectInt(Rect rect)
    {
        return new RectInt(
            (int)Math.Round(rect.X),
            (int)Math.Round(rect.Y),
            Math.Max(1, (int)Math.Round(rect.Width)),
            Math.Max(1, (int)Math.Round(rect.Height)));
    }

    private enum CropInteraction
    {
        None,
        Move,
        Resize
    }
}
