using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AA.Annotate.App.ViewModels;

public sealed class CommandBarViewModel : INotifyPropertyChanged
{
    private int _currentCaptureNumber = 1;
    private bool _isCaptureDropdownOpen;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int CurrentCaptureNumber
    {
        get => _currentCaptureNumber;
        set => SetField(ref _currentCaptureNumber, value);
    }

    public bool IsCaptureDropdownOpen
    {
        get => _isCaptureDropdownOpen;
        set => SetField(ref _isCaptureDropdownOpen, value);
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
