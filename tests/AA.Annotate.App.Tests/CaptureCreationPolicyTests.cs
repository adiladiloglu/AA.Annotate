using AA.Annotate.App.ViewModels;

namespace AA.Annotate.App.Tests;

public sealed class CaptureCreationPolicyTests
{
    [Theory]
    [InlineData(AnnotationInteractionMode.Idle, false, true)]
    [InlineData(AnnotationInteractionMode.Editing, false, true)]
    [InlineData(AnnotationInteractionMode.CaptureDropdownOpen, false, true)]
    [InlineData(AnnotationInteractionMode.DrawingAnnotation, false, false)]
    [InlineData(AnnotationInteractionMode.AnnotationSelected, false, false)]
    [InlineData(AnnotationInteractionMode.EditingCrop, true, false)]
    [InlineData(AnnotationInteractionMode.Idle, true, false)]
    public void CanUseCaptureControlsOnlyWhenNoEditToolIsActive(
        AnnotationInteractionMode mode,
        bool cropOverlayVisible,
        bool expected)
    {
        var canUseControls = CaptureCreationPolicy.CanUseCaptureControls(mode, cropOverlayVisible);

        Assert.Equal(expected, canUseControls);
    }
}
