namespace AA.Annotate.Platform;

public sealed class NoOpWindowIntegration : IWindowIntegration
{
    public void SuppressBorder(nint windowHandle)
    {
    }

    public IDisposable EnableTransparentHitTest(
        nint windowHandle,
        Func<int, int, bool> shouldHandleScreenPoint)
    {
        return EmptyDisposable.Instance;
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
