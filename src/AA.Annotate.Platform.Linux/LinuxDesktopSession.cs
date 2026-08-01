using System.Net.Sockets;

namespace AA.Annotate.Platform.Linux;

public enum LinuxDesktopSessionKind
{
    Unknown,
    X11,
    Wayland
}

public sealed record LinuxDesktopSession(
    LinuxDesktopSessionKind Kind,
    string? SessionType,
    string? X11Display,
    string? WaylandDisplay);

public interface ILinuxDesktopSessionDetector
{
    LinuxDesktopSession Detect();
}

public sealed class LinuxDesktopSessionDetector : ILinuxDesktopSessionDetector
{
    private static readonly TimeSpan WaylandSocketProbeTimeout = TimeSpan.FromMilliseconds(100);

    private readonly Func<string, string?> getEnvironmentVariable;
    private readonly Func<string, string?> findLiveWaylandDisplay;

    public LinuxDesktopSessionDetector()
        : this(Environment.GetEnvironmentVariable, FindLiveWaylandDisplay)
    {
    }

    internal LinuxDesktopSessionDetector(Func<string, string?> getEnvironmentVariable)
        : this(getEnvironmentVariable, FindLiveWaylandDisplay)
    {
    }

    internal LinuxDesktopSessionDetector(
        Func<string, string?> getEnvironmentVariable,
        Func<string, string?> findLiveWaylandDisplay)
    {
        this.getEnvironmentVariable = getEnvironmentVariable
            ?? throw new ArgumentNullException(nameof(getEnvironmentVariable));
        this.findLiveWaylandDisplay = findLiveWaylandDisplay
            ?? throw new ArgumentNullException(nameof(findLiveWaylandDisplay));
    }

    public LinuxDesktopSession Detect()
    {
        var sessionType = NormalizeSessionType(getEnvironmentVariable("XDG_SESSION_TYPE"));
        var x11Display = NormalizeDisplayName(getEnvironmentVariable("DISPLAY"));
        var waylandDisplay = NormalizeDisplayName(getEnvironmentVariable("WAYLAND_DISPLAY"));
        var runtimeDirectory = NormalizeDisplayName(getEnvironmentVariable("XDG_RUNTIME_DIR"));

        if (sessionType != "x11" && waylandDisplay is null && runtimeDirectory is not null)
        {
            waylandDisplay = NormalizeDisplayName(findLiveWaylandDisplay(runtimeDirectory));
        }

        var kind = sessionType switch
        {
            "x11" => LinuxDesktopSessionKind.X11,
            "wayland" => LinuxDesktopSessionKind.Wayland,
            _ when waylandDisplay is not null => LinuxDesktopSessionKind.Wayland,
            _ when x11Display is not null => LinuxDesktopSessionKind.X11,
            _ => LinuxDesktopSessionKind.Unknown
        };

        return new LinuxDesktopSession(kind, sessionType, x11Display, waylandDisplay);
    }

    private static string? FindLiveWaylandDisplay(string runtimeDirectory)
    {
        if (!OperatingSystem.IsLinux())
        {
            return null;
        }

        try
        {
            foreach (var path in Directory
                         .EnumerateFileSystemEntries(runtimeDirectory, "wayland-*", SearchOption.TopDirectoryOnly)
                         .Where(path => !path.EndsWith(".lock", StringComparison.Ordinal))
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                try
                {
                    using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                    using var cancellation = new CancellationTokenSource(WaylandSocketProbeTimeout);
                    socket.ConnectAsync(new UnixDomainSocketEndPoint(path), cancellation.Token)
                        .GetAwaiter()
                        .GetResult();

                    if (socket.Connected)
                    {
                        return Path.GetFileName(path);
                    }
                }
                catch (Exception exception) when (exception is SocketException
                                                      or IOException
                                                      or OperationCanceledException
                                                      or ArgumentException)
                {
                    // Stale socket paths and non-socket wayland-* entries are not live displays.
                }
            }
        }
        catch (Exception exception) when (exception is IOException
                                              or UnauthorizedAccessException
                                              or ArgumentException)
        {
            // An absent or inaccessible runtime directory cannot provide a fallback display.
        }

        return null;
    }

    private static string? NormalizeSessionType(string? value)
    {
        return NormalizeDisplayName(value)?.ToLowerInvariant();
    }

    private static string? NormalizeDisplayName(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
