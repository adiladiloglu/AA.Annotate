using AA.Annotate.App.ViewModels;

namespace AA.Annotate.App.Tests;

public sealed class OverlayPresentationPolicyTests
{
    public static TheoryData<AnnotationInteractionMode> PassiveModes =>
        new()
        {
            AnnotationInteractionMode.Idle,
            AnnotationInteractionMode.Editing,
            AnnotationInteractionMode.CaptureDropdownOpen
        };

    public static TheoryData<AnnotationInteractionMode> ActiveModes =>
        new()
        {
            AnnotationInteractionMode.DrawingAnnotation,
            AnnotationInteractionMode.DrawingPrivacyMask,
            AnnotationInteractionMode.EditingCrop,
            AnnotationInteractionMode.AnnotationSelected
        };

    public static TheoryData<AnnotationInteractionMode> TerminalModes =>
        new()
        {
            AnnotationInteractionMode.Finishing,
            AnnotationInteractionMode.Cancelled,
            AnnotationInteractionMode.Completed
        };

    [Theory]
    [MemberData(nameof(PassiveModes))]
    public void PassiveModeShowsOnlyEnabledToolbar(AnnotationInteractionMode mode)
    {
        var presentation = Create(mode: mode);

        Assert.Equal(
            new OverlayPresentation(
                OverlayVisible: false,
                CaptureSurfaceVisible: false,
                ToolbarVisible: true,
                ToolbarEnabled: true),
            presentation);
    }

    [Theory]
    [MemberData(nameof(ActiveModes))]
    public void ActiveModeShowsOverlayCaptureAndEnabledToolbar(AnnotationInteractionMode mode)
    {
        var presentation = Create(mode: mode);

        Assert.Equal(
            new OverlayPresentation(
                OverlayVisible: true,
                CaptureSurfaceVisible: true,
                ToolbarVisible: true,
                ToolbarEnabled: true),
            presentation);
    }

    [Fact]
    public void DrawingFlagShowsOverlayCaptureAndEnabledToolbar()
    {
        var presentation = Create(isDrawing: true);

        AssertActivePresentation(presentation);
    }

    [Fact]
    public void CropOverlayShowsOverlayCaptureAndEnabledToolbar()
    {
        var presentation = Create(cropOverlayVisible: true);

        AssertActivePresentation(presentation);
    }

    [Fact]
    public void CommentEditorShowsOverlayCaptureAndEnabledToolbar()
    {
        var presentation = Create(commentEditorVisible: true);

        AssertActivePresentation(presentation);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void ModalSurfaceShowsOverlayAndHidesToolbar(
        bool idleWarningVisible,
        bool sessionConfirmationVisible)
    {
        var presentation = Create(
            idleWarningVisible: idleWarningVisible,
            sessionConfirmationVisible: sessionConfirmationVisible);

        Assert.Equal(
            new OverlayPresentation(
                OverlayVisible: true,
                CaptureSurfaceVisible: false,
                ToolbarVisible: false,
                ToolbarEnabled: false),
            presentation);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ModalSurfaceRetainsCaptureSurfaceForUnderlyingEditingState(
        bool idleWarningVisible,
        bool sessionConfirmationVisible)
    {
        var presentation = Create(
            mode: AnnotationInteractionMode.DrawingAnnotation,
            idleWarningVisible: idleWarningVisible,
            sessionConfirmationVisible: sessionConfirmationVisible);

        Assert.Equal(
            new OverlayPresentation(
                OverlayVisible: true,
                CaptureSurfaceVisible: true,
                ToolbarVisible: false,
                ToolbarEnabled: false),
            presentation);
    }

    [Fact]
    public void CapturingFlagHidesBothTopLevels()
    {
        var presentation = Create(
            isCapturing: true,
            mode: AnnotationInteractionMode.DrawingAnnotation,
            idleWarningVisible: true);

        AssertHidden(presentation);
    }

    [Fact]
    public void CapturingModeHidesBothTopLevels()
    {
        var presentation = Create(
            mode: AnnotationInteractionMode.Capturing,
            cropOverlayVisible: true);

        AssertHidden(presentation);
    }

    [Fact]
    public void TerminalFlagHidesBothTopLevels()
    {
        var presentation = Create(
            isDrawing: true,
            idleWarningVisible: true,
            isTerminal: true);

        AssertHidden(presentation);
    }

    [Theory]
    [MemberData(nameof(TerminalModes))]
    public void TerminalModeHidesBothTopLevels(AnnotationInteractionMode mode)
    {
        var presentation = Create(
            mode: mode,
            isDrawing: true,
            sessionConfirmationVisible: true);

        AssertHidden(presentation);
    }

    private static OverlayPresentation Create(
        bool isCapturing = false,
        bool isDrawing = false,
        AnnotationInteractionMode mode = AnnotationInteractionMode.Idle,
        bool cropOverlayVisible = false,
        bool commentEditorVisible = false,
        bool idleWarningVisible = false,
        bool sessionConfirmationVisible = false,
        bool isTerminal = false)
    {
        return OverlayPresentationPolicy.Create(
            isCapturing,
            isDrawing,
            mode,
            cropOverlayVisible,
            commentEditorVisible,
            idleWarningVisible,
            sessionConfirmationVisible,
            isTerminal);
    }

    private static void AssertActivePresentation(OverlayPresentation presentation)
    {
        Assert.Equal(
            new OverlayPresentation(
                OverlayVisible: true,
                CaptureSurfaceVisible: true,
                ToolbarVisible: true,
                ToolbarEnabled: true),
            presentation);
    }

    private static void AssertHidden(OverlayPresentation presentation)
    {
        Assert.Equal(
            new OverlayPresentation(
                OverlayVisible: false,
                CaptureSurfaceVisible: false,
                ToolbarVisible: false,
                ToolbarEnabled: false),
            presentation);
    }
}
