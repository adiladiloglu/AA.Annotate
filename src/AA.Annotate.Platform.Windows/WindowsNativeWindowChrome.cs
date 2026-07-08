using System.Runtime.InteropServices;

namespace AA.Annotate.Platform.Windows;

public static class WindowsNativeWindowChrome
{
    private const int GwlStyle = -16;
    private const int GwlWndProc = -4;
    private const nint WsBorder = 0x00800000;
    private const nint WsDlgFrame = 0x00400000;
    private const nint WsThickFrame = 0x00040000;
    private const int WmNcHitTest = 0x0084;
    private const int HtTransparent = -1;
    private const int SwpNoSize = 0x0001;
    private const int SwpNoMove = 0x0002;
    private const int SwpNoZOrder = 0x0004;
    private const int SwpNoActivate = 0x0010;
    private const int SwpFrameChanged = 0x0020;
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwaBorderColor = 34;
    private const int DwmwcpDoNotRound = 1;
    private const uint DwmwaColorNone = 0xFFFFFFFE;
    private static readonly object HookGate = new();
    private static readonly Dictionary<nint, HitTestHook> HitTestHooks = [];

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

    public static IDisposable EnableTransparentHitTest(
        nint windowHandle,
        Func<int, int, bool> shouldHandleScreenPoint)
    {
        if (windowHandle == 0)
        {
            return EmptyDisposable.Instance;
        }

        lock (HookGate)
        {
            if (HitTestHooks.Remove(windowHandle, out var existing))
            {
                existing.Dispose();
            }

            var hook = new HitTestHook(windowHandle, shouldHandleScreenPoint);
            HitTestHooks[windowHandle] = hook;
            return hook;
        }
    }

    private static int GetSignedLowWord(nint value)
    {
        return (short)((long)value & 0xFFFF);
    }

    private static int GetSignedHighWord(nint value)
    {
        return (short)(((long)value >> 16) & 0xFFFF);
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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint CallWindowProc(
        nint lpPrevWndFunc,
        nint hWnd,
        uint msg,
        nint wParam,
        nint lParam);

    private delegate nint WindowProc(nint hWnd, uint msg, nint wParam, nint lParam);

    private sealed class HitTestHook : IDisposable
    {
        private readonly nint _windowHandle;
        private readonly Func<int, int, bool> _shouldHandleScreenPoint;
        private readonly WindowProc _windowProc;
        private readonly nint _previousWindowProc;
        private bool _isDisposed;

        public HitTestHook(nint windowHandle, Func<int, int, bool> shouldHandleScreenPoint)
        {
            _windowHandle = windowHandle;
            _shouldHandleScreenPoint = shouldHandleScreenPoint;
            _windowProc = WndProc;
            _previousWindowProc = SetWindowLongPtr(
                _windowHandle,
                GwlWndProc,
                Marshal.GetFunctionPointerForDelegate(_windowProc));
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            SetWindowLongPtr(_windowHandle, GwlWndProc, _previousWindowProc);
            lock (HookGate)
            {
                HitTestHooks.Remove(_windowHandle);
            }
        }

        private nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam)
        {
            if (msg == WmNcHitTest)
            {
                var screenX = GetSignedLowWord(lParam);
                var screenY = GetSignedHighWord(lParam);
                if (!_shouldHandleScreenPoint(screenX, screenY))
                {
                    return HtTransparent;
                }
            }

            return CallWindowProc(_previousWindowProc, hWnd, msg, wParam, lParam);
        }
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
