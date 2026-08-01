using System.Runtime.InteropServices;

namespace AA.Annotate.Platform.Linux;

internal static class LinuxX11Native
{
    internal const int LsbFirst = 0;
    internal const int ZPixmap = 2;
    internal const long MotifHintsDecorations = 1L << 1;
    internal const int PropertyModeReplace = 0;
    internal const int ClientMessage = 33;
    internal const long SubstructureNotifyMask = 1L << 19;
    internal const long SubstructureRedirectMask = 1L << 20;
    internal const long NetWmStateRemove = 0;
    internal const long NetWmStateAdd = 1;
    internal const long NetWmStateSourceApplication = 1;
    internal const ulong AllPlanes = ulong.MaxValue;

    [DllImport("libX11.so.6", EntryPoint = "XOpenDisplay")]
    internal static extern nint OpenDisplay(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string? displayName);

    [DllImport("libX11.so.6", EntryPoint = "XCloseDisplay")]
    internal static extern int CloseDisplay(nint display);

    [DllImport("libX11.so.6", EntryPoint = "XDefaultScreen")]
    internal static extern int DefaultScreen(nint display);

    [DllImport("libX11.so.6", EntryPoint = "XRootWindow")]
    internal static extern ulong RootWindow(nint display, int screenNumber);

    [DllImport("libX11.so.6", EntryPoint = "XGetGeometry")]
    internal static extern int GetGeometry(
        nint display,
        ulong drawable,
        out ulong root,
        out int x,
        out int y,
        out uint width,
        out uint height,
        out uint borderWidth,
        out uint depth);

    [DllImport("libX11.so.6", EntryPoint = "XGetImage")]
    internal static extern nint GetImage(
        nint display,
        ulong drawable,
        int x,
        int y,
        uint width,
        uint height,
        ulong planeMask,
        int format);

    [DllImport("libX11.so.6", EntryPoint = "XDestroyImage")]
    internal static extern int DestroyImage(nint image);

    [DllImport("libX11.so.6", EntryPoint = "XRaiseWindow")]
    internal static extern int RaiseWindow(nint display, ulong window);

    [DllImport("libX11.so.6", EntryPoint = "XFlush")]
    internal static extern int Flush(nint display);

    [DllImport("libX11.so.6", EntryPoint = "XSync")]
    internal static extern int Sync(nint display, [MarshalAs(UnmanagedType.Bool)] bool discard);

    [DllImport("libX11.so.6", EntryPoint = "XInternAtom")]
    internal static extern ulong InternAtom(
        nint display,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string atomName,
        [MarshalAs(UnmanagedType.Bool)] bool onlyIfExists);

    [DllImport("libX11.so.6", EntryPoint = "XChangeProperty")]
    internal static extern int ChangeProperty(
        nint display,
        ulong window,
        ulong property,
        ulong type,
        int format,
        int mode,
        nint data,
        int elementCount);

    [DllImport("libX11.so.6", EntryPoint = "XSendEvent")]
    internal static extern int SendEvent(
        nint display,
        ulong window,
        [MarshalAs(UnmanagedType.Bool)] bool propagate,
        nint eventMask,
        ref XEvent sendEvent);
}

[StructLayout(LayoutKind.Explicit, Size = 192)]
internal struct XEvent
{
    [FieldOffset(0)]
    internal XClientMessageEvent ClientMessage;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XClientMessageEvent
{
    internal int Type;
    internal ulong Serial;
    internal int SendEvent;
    internal nint Display;
    internal ulong Window;
    internal ulong MessageType;
    internal int Format;
    internal long Data0;
    internal long Data1;
    internal long Data2;
    internal long Data3;
    internal long Data4;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XImage
{
    internal int Width;
    internal int Height;
    internal int XOffset;
    internal int Format;
    internal nint Data;
    internal int ByteOrder;
    internal int BitmapUnit;
    internal int BitmapBitOrder;
    internal int BitmapPad;
    internal int Depth;
    internal int BytesPerLine;
    internal int BitsPerPixel;
    internal ulong RedMask;
    internal ulong GreenMask;
    internal ulong BlueMask;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MotifWmHints
{
    internal nuint Flags;
    internal nuint Functions;
    internal nuint Decorations;
    internal nint InputMode;
    internal nuint Status;
}
