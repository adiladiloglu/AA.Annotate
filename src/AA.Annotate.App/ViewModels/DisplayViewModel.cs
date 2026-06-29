using AA.Annotate.Platform;

namespace AA.Annotate.App.ViewModels;

public sealed class DisplayViewModel
{
    public DisplayViewModel(int number, DisplayDescriptor display, bool isCurrent)
    {
        Number = number;
        Display = display;
        IsCurrent = isCurrent;
    }

    public int Number { get; }

    public DisplayDescriptor Display { get; }

    public bool IsCurrent { get; }
}
