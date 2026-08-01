namespace AA.Annotate.Platform;

public interface IWindowIntegration
{
    void SuppressBorder(nint windowHandle);

    void SetAlwaysOnTop(nint windowHandle, bool enabled);

    void BringToFrontWithoutActivation(nint windowHandle);

    void FlushCompositor();
}
