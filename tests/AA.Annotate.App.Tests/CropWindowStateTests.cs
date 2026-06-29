using AA.Annotate.App.ViewModels;
using AA.Annotate.Core.Geometry;

namespace AA.Annotate.App.Tests;

public sealed class CropWindowStateTests
{
    [Fact]
    public void StoredCropKeepsFullscreenPreviewAfterCropOverlayCloses()
    {
        var viewport = new SizeInt(1536, 864);
        var storedCrop = new RectInt(320, 120, 760, 430);

        var useFullscreen = InteractionSurfacePolicy.ShouldUseFullscreen(
            isCapturing: false,
            isDrawing: false,
            AnnotationInteractionMode.Idle,
            cropOverlayVisible: false,
            commentEditorVisible: false,
            cropIsActive: CropViewport.IsCropped(storedCrop, viewport));

        Assert.True(useFullscreen);
    }

    [Fact]
    public void ConfiguredWindowSizeWinsOverStaleCompactBounds()
    {
        var viewport = ViewportSizeSelector.Select(
            configuredWidth: 1536,
            configuredHeight: 864,
            boundsWidth: 320,
            boundsHeight: 88,
            canvasWidth: 320,
            canvasHeight: 88,
            fallback: new SizeInt(1920, 1080));

        Assert.Equal(new SizeInt(1536, 864), viewport);
    }

    [Fact]
    public void AnnotationModeRefreshDoesNotResetStoredPixelCrop()
    {
        var capture = new CaptureViewModel(
            "capture",
            1,
            new("display", "display", new RectInt(0, 0, 2560, 1600), true),
            "screen.png",
            "thumb.png",
            new SizeInt(2560, 1600),
            new RectInt(0, 0, 2560, 1600),
            isSelected: true)
        {
            CropPixelRect = new RectInt(640, 320, 960, 640),
            ViewportSize = new SizeInt(1536, 960)
        };

        capture.CropRect = CaptureCropProjector.ToViewportCrop(capture);

        Assert.Equal(new RectInt(384, 192, 576, 384), capture.CropRect);
        Assert.True(CaptureCropProjector.IsCropped(capture));
        Assert.Equal(new RectInt(640, 320, 960, 640), capture.CropPixelRect);
    }

    [Fact]
    public void ActiveViewportCropCommitsBeforeAnnotationModeHidesCropOverlay()
    {
        var capture = new CaptureViewModel(
            "capture",
            1,
            new("display", "display", new RectInt(0, 0, 2560, 1600), true),
            "screen.png",
            "thumb.png",
            new SizeInt(2560, 1600),
            new RectInt(0, 0, 2560, 1600),
            isSelected: true)
        {
            ViewportSize = new SizeInt(1280, 800)
        };

        CaptureCropProjector.CommitViewportCrop(capture, new RectInt(320, 200, 640, 400));

        Assert.Equal(new RectInt(640, 400, 1280, 800), capture.CropPixelRect);
        Assert.True(CaptureCropProjector.IsCropped(capture));
    }
}
