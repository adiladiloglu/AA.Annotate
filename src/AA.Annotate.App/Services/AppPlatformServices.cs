using AA.Annotate.Platform;
using AA.Annotate.Platform.Linux;
using AA.Annotate.Platform.Windows;

namespace AA.Annotate.App.Services;

internal sealed record AppPlatformServices(
    IScreenCaptureService ScreenCapture,
    IWindowIntegration WindowIntegration)
{
    public static AppPlatformServices Create()
    {
        if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
        {
            return CreateWindowsServices();
        }

        if (OperatingSystem.IsLinux())
        {
            return new AppPlatformServices(
                new LinuxScreenCaptureService(),
                new LinuxX11WindowIntegration());
        }

        return new AppPlatformServices(
            new UnavailableScreenCaptureService(
                "Screen capture is not installed for this operating system."),
            new NoOpWindowIntegration());
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows6.1")]
    private static AppPlatformServices CreateWindowsServices()
    {
        return new AppPlatformServices(
            new WindowsScreenCaptureService(),
            new WindowsWindowIntegration());
    }
}
