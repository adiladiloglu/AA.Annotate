using AA.Annotate.Core.Geometry;
using AA.Annotate.Platform;
using AA.Annotate.Platform.Linux;

namespace AA.Annotate.Platform.Linux.Tests;

public sealed class LinuxScreenCaptureServiceTests
{
    [Theory]
    [InlineData(LinuxDesktopSessionKind.X11, "x11")]
    [InlineData(LinuxDesktopSessionKind.Wayland, "wayland")]
    public async Task CaptureRoutesToDetectedDesktopBackend(
        LinuxDesktopSessionKind sessionKind,
        string expectedMessage)
    {
        var service = new LinuxScreenCaptureService(
            new StubSessionDetector(sessionKind),
            new StubCaptureService(ScreenCaptureResult.Failed("x11")),
            new StubCaptureService(ScreenCaptureResult.Failed("wayland")));

        var result = await service.CaptureScreenAsync(CreateRequest());

        Assert.Equal(ScreenCaptureOutcome.Failed, result.Outcome);
        Assert.Equal(expectedMessage, result.ErrorMessage);
    }

    [Fact]
    public async Task CaptureReturnsUnavailableForUnknownSession()
    {
        var service = new LinuxScreenCaptureService(
            new StubSessionDetector(LinuxDesktopSessionKind.Unknown),
            new StubCaptureService(ScreenCaptureResult.Failed("x11")),
            new StubCaptureService(ScreenCaptureResult.Failed("wayland")));

        var result = await service.CaptureScreenAsync(CreateRequest());

        Assert.Equal(ScreenCaptureOutcome.Unavailable, result.Outcome);
        Assert.Contains("No supported Linux desktop session", result.ErrorMessage);
    }

    [Fact]
    public async Task CaptureDoesNotDetectOrInvokeBackendWhenAlreadyCancelled()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var detector = new CountingSessionDetector();
        var backend = new CountingCaptureService();
        var service = new LinuxScreenCaptureService(detector, backend, backend);

        var result = await service.CaptureScreenAsync(
            CreateRequest() with { CancellationToken = cancellation.Token });

        Assert.Equal(ScreenCaptureOutcome.Cancelled, result.Outcome);
        Assert.Equal(0, detector.CallCount);
        Assert.Equal(0, backend.CallCount);
    }

    private static ScreenCaptureRequest CreateRequest()
    {
        return new ScreenCaptureRequest(
            "capture.png",
            new DisplayDescriptor(
                "1",
                "Display",
                new RectInt(0, 0, 100, 100),
                IsPrimary: true));
    }

    private sealed class StubSessionDetector(LinuxDesktopSessionKind kind)
        : ILinuxDesktopSessionDetector
    {
        public LinuxDesktopSession Detect()
        {
            return new LinuxDesktopSession(kind, null, null, null);
        }
    }

    private sealed class CountingSessionDetector : ILinuxDesktopSessionDetector
    {
        public int CallCount { get; private set; }

        public LinuxDesktopSession Detect()
        {
            CallCount++;
            return new LinuxDesktopSession(LinuxDesktopSessionKind.X11, "x11", ":0", null);
        }
    }

    private sealed class StubCaptureService(ScreenCaptureResult result) : IScreenCaptureService
    {
        public Task<ScreenCaptureResult> CaptureScreenAsync(ScreenCaptureRequest request)
        {
            return Task.FromResult(result);
        }
    }

    private sealed class CountingCaptureService : IScreenCaptureService
    {
        public int CallCount { get; private set; }

        public Task<ScreenCaptureResult> CaptureScreenAsync(ScreenCaptureRequest request)
        {
            CallCount++;
            return Task.FromResult(ScreenCaptureResult.Failed());
        }
    }
}
