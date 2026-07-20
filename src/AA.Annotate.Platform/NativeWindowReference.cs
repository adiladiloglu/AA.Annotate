namespace AA.Annotate.Platform;

public sealed record NativeWindowReference(
    nint Handle,
    string HandleDescriptor);
