namespace AA.Annotate.Platform;

public interface IScreenCaptureService
{
    Task<ScreenCaptureResult> CaptureScreenAsync(ScreenCaptureRequest request);
}
