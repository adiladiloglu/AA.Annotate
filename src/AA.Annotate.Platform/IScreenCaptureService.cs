namespace AA.Annotate.Platform;

public interface IScreenCaptureService
{
    Task<CapturedScreen> CaptureScreenAsync(DisplayDescriptor display, string outputPath, CancellationToken cancellationToken = default);
}
