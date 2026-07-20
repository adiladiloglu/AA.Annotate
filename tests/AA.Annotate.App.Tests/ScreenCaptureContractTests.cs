using AA.Annotate.Core.Geometry;
using AA.Annotate.Platform;

namespace AA.Annotate.App.Tests;

public sealed class ScreenCaptureContractTests
{
    [Fact]
    public void RequestCarriesDestinationDisplayCursorPreferenceAndCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var display = new DisplayDescriptor(
            "display-1",
            "Display 1",
            new RectInt(10, 20, 1920, 1080),
            IsPrimary: true);

        var request = new ScreenCaptureRequest(
            "capture.png",
            display,
            IncludeCursor: true,
            cancellation.Token,
            new NativeWindowReference((nint)42, "TEST"));

        Assert.Equal("capture.png", request.DestinationPath);
        Assert.Same(display, request.PreferredDisplay);
        Assert.True(request.IncludeCursor);
        Assert.Equal(cancellation.Token, request.CancellationToken);
        Assert.Equal((nint)42, request.ParentWindow!.Handle);
        Assert.Equal("TEST", request.ParentWindow.HandleDescriptor);
    }

    [Theory]
    [InlineData(ScreenCaptureOutcome.Cancelled)]
    [InlineData(ScreenCaptureOutcome.PermissionDenied)]
    [InlineData(ScreenCaptureOutcome.DisplayDisconnected)]
    [InlineData(ScreenCaptureOutcome.RestartRequired)]
    [InlineData(ScreenCaptureOutcome.Unavailable)]
    [InlineData(ScreenCaptureOutcome.Failed)]
    public void IncompleteResultNeverContainsCapturedScreen(ScreenCaptureOutcome outcome)
    {
        var result = outcome switch
        {
            ScreenCaptureOutcome.Cancelled => ScreenCaptureResult.Cancelled("cancelled"),
            ScreenCaptureOutcome.PermissionDenied => ScreenCaptureResult.PermissionDenied("denied"),
            ScreenCaptureOutcome.DisplayDisconnected => ScreenCaptureResult.DisplayDisconnected("disconnected"),
            ScreenCaptureOutcome.RestartRequired => ScreenCaptureResult.RestartRequired("restart"),
            ScreenCaptureOutcome.Unavailable => ScreenCaptureResult.Unavailable("unavailable"),
            ScreenCaptureOutcome.Failed => ScreenCaptureResult.Failed("failed"),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome))
        };

        Assert.Equal(outcome, result.Outcome);
        Assert.False(result.IsCompleted);
        Assert.Null(result.CapturedScreen);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void CompletedResultContainsAuthoritativeCaptureMetadata()
    {
        var display = new DisplayDescriptor(
            "display-1",
            "Display 1",
            new RectInt(0, 0, 2560, 1440),
            IsPrimary: true);
        var captured = new CapturedScreen(
            display,
            "capture.png",
            new SizeInt(5120, 2880));

        var result = ScreenCaptureResult.Completed(captured);

        Assert.Equal(ScreenCaptureOutcome.Completed, result.Outcome);
        Assert.True(result.IsCompleted);
        Assert.Same(captured, result.CapturedScreen);
        Assert.Null(result.ErrorMessage);
    }
}
