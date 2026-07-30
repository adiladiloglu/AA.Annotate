using System.Collections.ObjectModel;

namespace AA.Annotate.App.ViewModels;

public sealed class AnnotationSessionViewModel
{
    public CommandBarViewModel CommandBar { get; } = new();

    public ObservableCollection<CaptureViewModel> Captures { get; } = [];

    public AnnotationInteractionMode Mode { get; set; } = AnnotationInteractionMode.Editing;

    public CaptureViewModel? CurrentCapture { get; set; }

    public AnnotationViewModel? SelectedAnnotation { get; set; }
}
