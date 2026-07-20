namespace AA.Annotate.Platform;

public sealed class UnavailableScreenCaptureService(string reason) : IScreenCaptureService
{
    public Task<ScreenCaptureResult> CaptureScreenAsync(ScreenCaptureRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.FromResult(
            request.CancellationToken.IsCancellationRequested
                ? ScreenCaptureResult.Cancelled()
                : ScreenCaptureResult.Unavailable(reason));
    }
}
