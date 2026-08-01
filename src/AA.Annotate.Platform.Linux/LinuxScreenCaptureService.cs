using AA.Annotate.Platform;

namespace AA.Annotate.Platform.Linux;

public sealed class LinuxScreenCaptureService : IScreenCaptureService
{
    private readonly ILinuxDesktopSessionDetector sessionDetector;
    private readonly IScreenCaptureService x11Backend;
    private readonly IScreenCaptureService waylandBackend;

    public LinuxScreenCaptureService()
        : this(
            new LinuxDesktopSessionDetector(),
            new LinuxX11ScreenCaptureService(),
            new XdgPortalScreenCaptureService())
    {
    }

    public LinuxScreenCaptureService(
        ILinuxDesktopSessionDetector sessionDetector,
        IScreenCaptureService x11Backend,
        IScreenCaptureService waylandBackend)
    {
        this.sessionDetector = sessionDetector ?? throw new ArgumentNullException(nameof(sessionDetector));
        this.x11Backend = x11Backend ?? throw new ArgumentNullException(nameof(x11Backend));
        this.waylandBackend = waylandBackend ?? throw new ArgumentNullException(nameof(waylandBackend));
    }

    public Task<ScreenCaptureResult> CaptureScreenAsync(ScreenCaptureRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.CancellationToken.IsCancellationRequested)
        {
            return Task.FromResult(ScreenCaptureResult.Cancelled());
        }

        var session = sessionDetector.Detect();
        return session.Kind switch
        {
            LinuxDesktopSessionKind.X11 => x11Backend.CaptureScreenAsync(request),
            LinuxDesktopSessionKind.Wayland => waylandBackend.CaptureScreenAsync(request),
            _ => Task.FromResult(ScreenCaptureResult.Unavailable(
                "No supported Linux desktop session was detected. XDG_SESSION_TYPE, DISPLAY, and WAYLAND_DISPLAY were empty or unrecognized."))
        };
    }
}
