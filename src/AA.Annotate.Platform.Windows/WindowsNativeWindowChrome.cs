using System.Runtime.InteropServices;

namespace AA.Annotate.Platform.Windows;

public static class WindowsNativeWindowChrome
{
    private const int GwlStyle = -16;
    private const nint WsBorder = 0x00800000;
    private const nint WsDlgFrame = 0x00400000;
    private const nint WsThickFrame = 0x00040000;
    private const int SwpNoSize = 0x0001;
    private const int SwpNoMove = 0x0002;
    private const int SwpNoZOrder = 0x0004;
    private const int SwpNoActivate = 0x0010;
    private const int SwpFrameChanged = 0x0020;
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwaBorderColor = 34;
    private const int DwmwcpDoNotRound = 1;
    private const uint DwmwaColorNone = 0xFFFFFFFE;

    public static void SuppressBorder(nint windowHandle)
    {
        if (windowHandle == 0)
        {
            return;
        }

        var style = GetWindowLongPtr(windowHandle, GwlStyle);
        style &= ~(WsBorder | WsDlgFrame | WsThickFrame);
        SetWindowLongPtr(windowHandle, GwlStyle, style);
        SetWindowPos(
            windowHandle,
            0,
            0,
            0,
            0,
            0,
            SwpNoSize | SwpNoMove | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);

        var cornerPreference = DwmwcpDoNotRound;
        _ = DwmSetWindowAttribute(
            windowHandle,
            DwmwaWindowCornerPreference,
            ref cornerPreference,
            Marshal.SizeOf<int>());

        var borderColor = DwmwaColorNone;
        _ = DwmSetWindowAttribute(
            windowHandle,
            DwmwaBorderColor,
            ref borderColor,
            Marshal.SizeOf<uint>());
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
    private static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        nint hWnd,
        nint hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        int uFlags);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        nint hwnd,
        int attribute,
        ref int pvAttribute,
        int cbAttribute);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        nint hwnd,
        int attribute,
        ref uint pvAttribute,
        int cbAttribute);
}
