using System.Net.Sockets;
using AA.Annotate.Platform.Linux;

namespace AA.Annotate.Platform.Linux.Tests;

public sealed class LinuxDesktopSessionDetectorTests
{
    [Theory]
    [InlineData("x11", ":0", null, LinuxDesktopSessionKind.X11)]
    [InlineData("X11", ":0", "wayland-0", LinuxDesktopSessionKind.X11)]
    [InlineData("wayland", ":0", "Wayland-Custom", LinuxDesktopSessionKind.Wayland)]
    [InlineData("WAYLAND", ":0", null, LinuxDesktopSessionKind.Wayland)]
    [InlineData(null, ":1", null, LinuxDesktopSessionKind.X11)]
    [InlineData(null, ":1", "wayland-1", LinuxDesktopSessionKind.Wayland)]
    [InlineData(null, null, "wayland-1", LinuxDesktopSessionKind.Wayland)]
    [InlineData(null, null, null, LinuxDesktopSessionKind.Unknown)]
    [InlineData("tty", ":0", null, LinuxDesktopSessionKind.X11)]
    public void DetectUsesDeclaredSessionThenDesktopDisplayVariables(
        string? sessionType,
        string? display,
        string? waylandDisplay,
        LinuxDesktopSessionKind expected)
    {
        var environment = new Dictionary<string, string?>
        {
            ["XDG_SESSION_TYPE"] = sessionType,
            ["DISPLAY"] = display,
            ["WAYLAND_DISPLAY"] = waylandDisplay
        };
        var detector = new LinuxDesktopSessionDetector(
            name => environment.GetValueOrDefault(name));

        var session = detector.Detect();

        Assert.Equal(expected, session.Kind);
        Assert.Equal(sessionType?.ToLowerInvariant(), session.SessionType);
        Assert.Equal(display, session.X11Display);
        Assert.Equal(waylandDisplay, session.WaylandDisplay);
    }

    [Fact]
    public void DetectTreatsWhitespaceAsAbsent()
    {
        var detector = new LinuxDesktopSessionDetector(_ => "   ");

        var session = detector.Detect();

        Assert.Equal(LinuxDesktopSessionKind.Unknown, session.Kind);
        Assert.Null(session.SessionType);
        Assert.Null(session.X11Display);
        Assert.Null(session.WaylandDisplay);
    }

    [Fact]
    public void DetectUsesLiveRuntimeWaylandSocketBeforeImplicitX11Fallback()
    {
        var environment = new Dictionary<string, string?>
        {
            ["DISPLAY"] = ":0",
            ["XDG_RUNTIME_DIR"] = "/run/user/1000"
        };
        var detector = new LinuxDesktopSessionDetector(
            name => environment.GetValueOrDefault(name),
            runtimeDirectory => runtimeDirectory == "/run/user/1000" ? "wayland-0" : null);

        var session = detector.Detect();

        Assert.Equal(LinuxDesktopSessionKind.Wayland, session.Kind);
        Assert.Equal(":0", session.X11Display);
        Assert.Equal("wayland-0", session.WaylandDisplay);
    }

    [Fact]
    public void DetectKeepsExplicitX11SessionEvenWhenRuntimeHasLiveWaylandSocket()
    {
        var probeCalls = 0;
        var environment = new Dictionary<string, string?>
        {
            ["XDG_SESSION_TYPE"] = "x11",
            ["DISPLAY"] = ":0",
            ["XDG_RUNTIME_DIR"] = "/run/user/1000"
        };
        var detector = new LinuxDesktopSessionDetector(
            name => environment.GetValueOrDefault(name),
            _ =>
            {
                probeCalls++;
                return "wayland-0";
            });

        var session = detector.Detect();

        Assert.Equal(LinuxDesktopSessionKind.X11, session.Kind);
        Assert.Null(session.WaylandDisplay);
        Assert.Equal(0, probeCalls);
    }

    [Fact]
    public void DetectCompletesDeclaredWaylandSessionFromRuntimeSocket()
    {
        var environment = new Dictionary<string, string?>
        {
            ["XDG_SESSION_TYPE"] = "wayland",
            ["DISPLAY"] = ":0",
            ["XDG_RUNTIME_DIR"] = "/run/user/1000"
        };
        var detector = new LinuxDesktopSessionDetector(
            name => environment.GetValueOrDefault(name),
            _ => "wayland-1");

        var session = detector.Detect();

        Assert.Equal(LinuxDesktopSessionKind.Wayland, session.Kind);
        Assert.Equal("wayland-1", session.WaylandDisplay);
    }

    [Fact]
    public void DetectFindsActuallyListeningWaylandSocketOnLinux()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        var runtimeDirectory = Path.Combine(
            Path.GetTempPath(),
            $"aa-annotate-session-detector-{Guid.NewGuid():N}");
        Directory.CreateDirectory(runtimeDirectory);
        var socketPath = Path.Combine(runtimeDirectory, "wayland-7");

        try
        {
            using var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            listener.Bind(new UnixDomainSocketEndPoint(socketPath));
            listener.Listen(1);

            var detector = CreateRuntimeDetector(runtimeDirectory);

            var session = detector.Detect();

            Assert.Equal(LinuxDesktopSessionKind.Wayland, session.Kind);
            Assert.Equal("wayland-7", session.WaylandDisplay);
        }
        finally
        {
            Directory.Delete(runtimeDirectory, recursive: true);
        }
    }

    [Fact]
    public void DetectIgnoresStaleWaylandSocketOnLinux()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        var runtimeDirectory = Path.Combine(
            Path.GetTempPath(),
            $"aa-annotate-session-detector-{Guid.NewGuid():N}");
        Directory.CreateDirectory(runtimeDirectory);
        var socketPath = Path.Combine(runtimeDirectory, "wayland-7");

        try
        {
            using (var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
            {
                listener.Bind(new UnixDomainSocketEndPoint(socketPath));
            }

            var detector = CreateRuntimeDetector(runtimeDirectory);

            var session = detector.Detect();

            Assert.Equal(LinuxDesktopSessionKind.X11, session.Kind);
            Assert.Null(session.WaylandDisplay);
        }
        finally
        {
            Directory.Delete(runtimeDirectory, recursive: true);
        }
    }

    private static LinuxDesktopSessionDetector CreateRuntimeDetector(string runtimeDirectory)
    {
        var environment = new Dictionary<string, string?>
        {
            ["DISPLAY"] = ":0",
            ["XDG_RUNTIME_DIR"] = runtimeDirectory
        };

        return new LinuxDesktopSessionDetector(name => environment.GetValueOrDefault(name));
    }
}
