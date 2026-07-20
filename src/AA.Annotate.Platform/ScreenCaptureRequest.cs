namespace AA.Annotate.Platform;

public sealed record ScreenCaptureRequest(
    string DestinationPath,
    DisplayDescriptor? PreferredDisplay,
    bool IncludeCursor = false,
    CancellationToken CancellationToken = default,
    NativeWindowReference? ParentWindow = null);
