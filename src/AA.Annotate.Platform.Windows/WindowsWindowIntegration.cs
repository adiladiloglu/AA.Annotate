using AA.Annotate.Platform;

namespace AA.Annotate.Platform.Windows;

public sealed class WindowsWindowIntegration : IWindowIntegration
{
    public void SuppressBorder(nint windowHandle)
    {
        WindowsNativeWindowChrome.SuppressBorder(windowHandle);
    }

    public void SetAlwaysOnTop(nint windowHandle, bool enabled)
    {
        WindowsNativeWindowChrome.SetAlwaysOnTop(windowHandle, enabled);
    }

    public void BringToFrontWithoutActivation(nint windowHandle)
    {
        WindowsNativeWindowChrome.BringToFrontWithoutActivation(windowHandle);
    }

    public void FlushCompositor()
    {
        WindowsNativeWindowChrome.FlushCompositor();
    }
}
