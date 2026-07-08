using System.ComponentModel;
using System.Runtime.CompilerServices;
using AA.Annotate.Core.Geometry;

namespace AA.Annotate.App.ViewModels;

public sealed class PrivacyMaskViewModel : INotifyPropertyChanged
{
    private RectInt _boxRect;
    private bool _isSelected;

    public PrivacyMaskViewModel(string maskId, RectInt boxRect)
    {
        MaskId = maskId;
        _boxRect = boxRect;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string MaskId { get; }

    public RectInt BoxRect
    {
        get => _boxRect;
        set => SetField(ref _boxRect, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
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
