using AA.Annotate.App.ViewModels;
using Avalonia.Input;

namespace AA.Annotate.App.Tests;

public sealed class OverlayCreationGesturePolicyTests
{
    [Theory]
    [InlineData(
        AnnotationInteractionMode.DrawingAnnotation,
        "Ctrl+drag to draw a new annotation through this box")]
    [InlineData(
        AnnotationInteractionMode.DrawingPrivacyMask,
        "Ctrl+drag to draw a new privacy mask through this box")]
    public void DrawingModesExposeExistingBoxHint(
        AnnotationInteractionMode mode,
        string expected)
    {
        Assert.Equal(expected, OverlayCreationGesturePolicy.GetExistingBoxHint(mode));
    }

    [Theory]
    [InlineData(AnnotationInteractionMode.Idle)]
    [InlineData(AnnotationInteractionMode.AnnotationSelected)]
    [InlineData(AnnotationInteractionMode.EditingCrop)]
    public void NonDrawingModesHideExistingBoxHint(AnnotationInteractionMode mode)
    {
        Assert.Null(OverlayCreationGesturePolicy.GetExistingBoxHint(mode));
    }

    [Theory]
    [InlineData(AnnotationInteractionMode.DrawingAnnotation)]
    [InlineData(AnnotationInteractionMode.DrawingPrivacyMask)]
    public void CtrlLeftDragForcesNewBoxInDrawingModes(AnnotationInteractionMode mode)
    {
        var result = OverlayCreationGesturePolicy.ShouldForceNewBox(
            mode,
            KeyModifiers.Control,
            isLeftButtonPressed: true);

        Assert.True(result);
    }

    [Fact]
    public void AdditionalModifiersDoNotDisableCtrlDrag()
    {
        var result = OverlayCreationGesturePolicy.ShouldForceNewBox(
            AnnotationInteractionMode.DrawingAnnotation,
            KeyModifiers.Control | KeyModifiers.Shift,
            isLeftButtonPressed: true);

        Assert.True(result);
    }

    [Theory]
    [InlineData(AnnotationInteractionMode.Idle, KeyModifiers.Control, true)]
    [InlineData(AnnotationInteractionMode.AnnotationSelected, KeyModifiers.Control, true)]
    [InlineData(AnnotationInteractionMode.DrawingAnnotation, KeyModifiers.None, true)]
    [InlineData(AnnotationInteractionMode.DrawingPrivacyMask, KeyModifiers.Control, false)]
    public void OtherGesturesKeepExistingHitBehavior(
        AnnotationInteractionMode mode,
        KeyModifiers modifiers,
        bool isLeftButtonPressed)
    {
        var result = OverlayCreationGesturePolicy.ShouldForceNewBox(
            mode,
            modifiers,
            isLeftButtonPressed);

        Assert.False(result);
    }
}
