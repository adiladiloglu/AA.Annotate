namespace AA.Annotate.Platform;

public interface IWindowIntegration
{
    void SuppressBorder(nint windowHandle);

    IDisposable EnableTransparentHitTest(
        nint windowHandle,
        Func<int, int, bool> shouldHandleScreenPoint);
}
