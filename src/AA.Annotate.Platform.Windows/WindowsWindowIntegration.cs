using AA.Annotate.Platform;

namespace AA.Annotate.Platform.Windows;

public sealed class WindowsWindowIntegration : IWindowIntegration
{
    public void SuppressBorder(nint windowHandle)
    {
        WindowsNativeWindowChrome.SuppressBorder(windowHandle);
    }

    public IDisposable EnableTransparentHitTest(
        nint windowHandle,
        Func<int, int, bool> shouldHandleScreenPoint)
    {
        return WindowsNativeWindowChrome.EnableTransparentHitTest(
            windowHandle,
            shouldHandleScreenPoint);
    }
}
