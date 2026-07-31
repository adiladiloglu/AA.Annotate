namespace AA.Annotate.Platform;

public sealed class NoOpWindowIntegration : IWindowIntegration
{
    public void SuppressBorder(nint windowHandle)
    {
    }

    public void BringToFrontWithoutActivation(nint windowHandle)
    {
    }

    public void FlushCompositor()
    {
    }
}
