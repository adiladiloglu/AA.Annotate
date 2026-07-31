namespace AA.Annotate.Platform;

public interface IWindowIntegration
{
    void SuppressBorder(nint windowHandle);

    void BringToFrontWithoutActivation(nint windowHandle);

    void FlushCompositor();
}
