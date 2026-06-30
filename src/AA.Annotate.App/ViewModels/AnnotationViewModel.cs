using System.ComponentModel;
using System.Runtime.CompilerServices;
using AA.Annotate.Core.Geometry;
using AA.Annotate.Core.Services;

namespace AA.Annotate.App.ViewModels;

public sealed class AnnotationViewModel : INotifyPropertyChanged
{
    private RectInt _boxRect;
    private string _comment;
    private bool _isSelected;
    private AnnotationCropExportState _exportState = AnnotationCropExportState.Included;
    private bool _isPendingComment;

    public AnnotationViewModel(string annotationId, int number, RectInt boxRect, string comment, bool isPendingComment = false)
    {
        AnnotationId = annotationId;
        Number = number;
        _boxRect = boxRect;
        _comment = comment;
        _isPendingComment = isPendingComment;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string AnnotationId { get; }

    public int Number { get; }

    public RectInt BoxRect
    {
        get => _boxRect;
        set => SetField(ref _boxRect, value);
    }

    public string Comment
    {
        get => _comment;
        set => SetField(ref _comment, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    public AnnotationCropExportState ExportState
    {
        get => _exportState;
        set => SetField(ref _exportState, value);
    }

    public bool IsPendingComment
    {
        get => _isPendingComment;
        set => SetField(ref _isPendingComment, value);
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
