using AA.Annotate.Core.Geometry;
using AA.Annotate.Core.Services;
using AA.Annotate.Platform;
using SkiaSharp;
using Tmds.DBus;

namespace AA.Annotate.Platform.Linux;

public sealed class XdgPortalScreenCaptureService : IScreenCaptureService
{
    private const string PortalService = "org.freedesktop.portal.Desktop";
    private static readonly ObjectPath PortalPath = new("/org/freedesktop/portal/desktop");
    private static readonly TimeSpan PortalTimeout = TimeSpan.FromMinutes(2);

    public async Task<ScreenCaptureResult> CaptureScreenAsync(ScreenCaptureRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!OperatingSystem.IsLinux())
        {
            return ScreenCaptureResult.Unavailable(
                "The XDG Screenshot portal is available only on Linux.");
        }

        if (request.PreferredDisplay is not { } display)
        {
            return ScreenCaptureResult.Unavailable(
                "Wayland screen capture requires a preferred display.");
        }

        if (request.IncludeCursor)
        {
            return ScreenCaptureResult.Unavailable(
                "Wayland screen capture does not support including the cursor.");
        }

        var partialPath = $"{request.DestinationPath}.{Guid.NewGuid():N}.partial";
        try
        {
            request.CancellationToken.ThrowIfCancellationRequested();

            using var connection = new Connection(Address.Session);
            var connectionInfo = await connection.ConnectAsync().ConfigureAwait(false);

            var token = $"aa_annotate_{Guid.NewGuid():N}";
            var expectedPath = CreateExpectedRequestPath(connectionInfo.LocalName, token);
            var screenshot = connection.CreateProxy<IPortalScreenshot>(
                PortalService,
                PortalPath);
            var responseCompletion =
                new TaskCompletionSource<PortalResponse>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
            var responseRequest = connection.CreateProxy<IPortalRequest>(
                PortalService,
                expectedPath);
            using var subscription = await responseRequest.WatchResponseAsync(
                response => responseCompletion.TrySetResult(
                    new PortalResponse(
                        response.Response,
                        new Dictionary<string, object>(response.Results))),
                exception => responseCompletion.TrySetException(exception))
                .ConfigureAwait(false);

            var options = new Dictionary<string, object>
            {
                ["handle_token"] = token,
                ["interactive"] = false,
                ["modal"] = true
            };
            var returnedPath = await screenshot.ScreenshotAsync(
                    CreateParentWindowIdentifier(request.ParentWindow),
                    options)
                .ConfigureAwait(false);

            if (returnedPath != expectedPath)
            {
                return ScreenCaptureResult.Unavailable(
                    "The desktop screenshot portal returned an unexpected request handle.");
            }

            using var timeout = new CancellationTokenSource(PortalTimeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                request.CancellationToken,
                timeout.Token);

            PortalResponse response;
            try
            {
                response = await responseCompletion.Task
                    .WaitAsync(linked.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (request.CancellationToken.IsCancellationRequested)
            {
                await TryCloseRequestAsync(responseRequest).ConfigureAwait(false);
                return ScreenCaptureResult.Cancelled();
            }
            catch (OperationCanceledException)
            {
                await TryCloseRequestAsync(responseRequest).ConfigureAwait(false);
                return ScreenCaptureResult.Unavailable(
                    "The desktop screenshot portal did not respond in time.");
            }

            var decision = XdgPortalResponseMapper.Evaluate(
                response.Response,
                response.Results);
            if (decision.Kind == XdgPortalResponseKind.Cancelled)
            {
                return ScreenCaptureResult.Cancelled(decision.Message);
            }

            if (decision.Kind == XdgPortalResponseKind.Unavailable ||
                decision.LocalPath is not { } portalPath)
            {
                return ScreenCaptureResult.Unavailable(decision.Message);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(request.DestinationPath) ?? ".");
            await CopyFileAsync(
                    portalPath,
                    partialPath,
                    request.CancellationToken)
                .ConfigureAwait(false);

            using var bitmap = SKBitmap.Decode(partialPath);
            if (bitmap is null || bitmap.Width <= 0 || bitmap.Height <= 0)
            {
                return ScreenCaptureResult.Failed(
                    "The desktop screenshot portal returned an unreadable image.");
            }

            if (bitmap.Width != display.Bounds.Width ||
                bitmap.Height != display.Bounds.Height)
            {
                return ScreenCaptureResult.DisplayDisconnected(
                    "Wayland capture currently requires one active display whose size matches the selected display.");
            }

            request.CancellationToken.ThrowIfCancellationRequested();
            File.Move(partialPath, request.DestinationPath, overwrite: true);
            PrivateFileSystem.ProtectFile(request.DestinationPath);
            return ScreenCaptureResult.Completed(new CapturedScreen(
                display,
                request.DestinationPath,
                new SizeInt(bitmap.Width, bitmap.Height)));
        }
        catch (OperationCanceledException)
        {
            return ScreenCaptureResult.Cancelled();
        }
        catch (Exception exception) when (IsAccessDenied(exception))
        {
            return ScreenCaptureResult.PermissionDenied(
                "The desktop denied screenshot access. Allow the screenshot request and try again.");
        }
        catch (Exception exception) when (IsPortalUnavailable(exception))
        {
            return ScreenCaptureResult.Unavailable(
                "The XDG desktop Screenshot portal is not available in this graphical session.");
        }
        catch (IOException)
        {
            return ScreenCaptureResult.Failed(
                "The captured image could not be copied into the private session.");
        }
        catch (Exception exception)
        {
            return ScreenCaptureResult.Failed(
                $"The desktop screenshot request failed unexpectedly ({exception.GetType().Name}: {exception.Message}).");
        }
        finally
        {
            TryDelete(partialPath);
        }
    }

    private static ObjectPath CreateExpectedRequestPath(string? uniqueName, string token)
    {
        if (string.IsNullOrWhiteSpace(uniqueName) || uniqueName[0] != ':')
        {
            throw new InvalidOperationException(
                "The session bus did not assign a unique client name.");
        }

        var sender = uniqueName[1..].Replace('.', '_');
        return new ObjectPath(
            $"/org/freedesktop/portal/desktop/request/{sender}/{token}");
    }

    private static string CreateParentWindowIdentifier(NativeWindowReference? parent)
    {
        if (parent is null ||
            parent.Handle == 0 ||
            !parent.HandleDescriptor.Contains("XID", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return $"x11:{unchecked((ulong)parent.Handle):x}";
    }

    private static async Task CopyFileAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        await using var source = new FileStream(
            sourcePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        await using var destination = PrivateFileSystem.CreateFile(destinationPath);
        await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task TryCloseRequestAsync(IPortalRequest request)
    {
        try
        {
            await request.CloseAsync().ConfigureAwait(false);
        }
        catch
        {
            // Cancellation cleanup is best effort; the connection is disposed next.
        }
    }

    private static bool IsAccessDenied(Exception exception)
    {
        return exception.Message.Contains("AccessDenied", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("NotAllowed", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPortalUnavailable(Exception exception)
    {
        return exception.Message.Contains("ServiceUnknown", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("NameHasNoOwner", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("No such interface", StringComparison.OrdinalIgnoreCase);
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
            // A failed partial cleanup must not hide the capture outcome.
        }
    }

    private sealed record PortalResponse(
        uint Response,
        IReadOnlyDictionary<string, object> Results);
}

[DBusInterface("org.freedesktop.portal.Screenshot")]
public interface IPortalScreenshot : IDBusObject
{
    Task<ObjectPath> ScreenshotAsync(
        string parentWindow,
        IDictionary<string, object> options);
}

[DBusInterface("org.freedesktop.portal.Request")]
public interface IPortalRequest : IDBusObject
{
    Task CloseAsync();

    Task<IDisposable> WatchResponseAsync(
        Action<(uint Response, IDictionary<string, object> Results)> handler,
        Action<Exception>? onError = null);
}

internal enum XdgPortalResponseKind
{
    Completed,
    Cancelled,
    Unavailable
}

internal sealed record XdgPortalResponseDecision(
    XdgPortalResponseKind Kind,
    string? LocalPath,
    string? Message);

internal static class XdgPortalResponseMapper
{
    internal static XdgPortalResponseDecision Evaluate(
        uint response,
        IReadOnlyDictionary<string, object> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        if (response == 1)
        {
            return new XdgPortalResponseDecision(
                XdgPortalResponseKind.Cancelled,
                LocalPath: null,
                "The desktop screenshot request was cancelled.");
        }

        if (response != 0)
        {
            return new XdgPortalResponseDecision(
                XdgPortalResponseKind.Unavailable,
                LocalPath: null,
                "The desktop screenshot portal ended the request without returning an image. " +
                "On GNOME Wayland, log in with the X11 session if this continues.");
        }

        if (!results.TryGetValue("uri", out var value) ||
            value is not string uriText ||
            !Uri.TryCreate(uriText, UriKind.Absolute, out var uri) ||
            !uri.IsFile ||
            !Path.IsPathFullyQualified(uri.LocalPath) ||
            !File.Exists(uri.LocalPath))
        {
            return new XdgPortalResponseDecision(
                XdgPortalResponseKind.Unavailable,
                LocalPath: null,
                "The desktop screenshot portal did not return a local image.");
        }

        return new XdgPortalResponseDecision(
            XdgPortalResponseKind.Completed,
            uri.LocalPath,
            Message: null);
    }
}
