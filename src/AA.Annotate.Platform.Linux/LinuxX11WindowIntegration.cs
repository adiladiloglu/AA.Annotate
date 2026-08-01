using System.Runtime.InteropServices;
using AA.Annotate.Platform;

namespace AA.Annotate.Platform.Linux;

public sealed class LinuxX11WindowIntegration : IWindowIntegration
{
    public void SuppressBorder(nint windowHandle)
    {
        if (!OperatingSystem.IsLinux() || windowHandle == 0)
        {
            return;
        }

        WithDisplay(display =>
        {
            var motifHints = LinuxX11Native.InternAtom(
                display,
                "_MOTIF_WM_HINTS",
                onlyIfExists: false);
            if (motifHints == 0)
            {
                return;
            }

            var hints = new MotifWmHints
            {
                Flags = (nuint)LinuxX11Native.MotifHintsDecorations,
                Decorations = 0
            };
            var data = Marshal.AllocHGlobal(Marshal.SizeOf<MotifWmHints>());
            try
            {
                Marshal.StructureToPtr(hints, data, fDeleteOld: false);
                _ = LinuxX11Native.ChangeProperty(
                    display,
                    unchecked((ulong)windowHandle),
                    motifHints,
                    motifHints,
                    format: 32,
                    LinuxX11Native.PropertyModeReplace,
                    data,
                    elementCount: 5);
                _ = LinuxX11Native.Flush(display);
            }
            finally
            {
                Marshal.FreeHGlobal(data);
            }
        });
    }

    public void BringToFrontWithoutActivation(nint windowHandle)
    {
        if (!OperatingSystem.IsLinux() || windowHandle == 0)
        {
            return;
        }

        WithDisplay(display =>
        {
            _ = LinuxX11Native.RaiseWindow(display, unchecked((ulong)windowHandle));
            _ = LinuxX11Native.Flush(display);
        });
    }

    public void SetAlwaysOnTop(nint windowHandle, bool enabled)
    {
        if (!OperatingSystem.IsLinux() || windowHandle == 0)
        {
            return;
        }

        WithDisplay(display =>
        {
            var screen = LinuxX11Native.DefaultScreen(display);
            var root = LinuxX11Native.RootWindow(display, screen);
            var state = LinuxX11Native.InternAtom(display, "_NET_WM_STATE", onlyIfExists: false);
            var above = LinuxX11Native.InternAtom(display, "_NET_WM_STATE_ABOVE", onlyIfExists: false);
            var staysOnTop = LinuxX11Native.InternAtom(
                display,
                "_NET_WM_STATE_STAYS_ON_TOP",
                onlyIfExists: false);
            if (root == 0 || state == 0 || above == 0)
            {
                return;
            }

            var message = CreateAlwaysOnTopMessage(
                display,
                windowHandle,
                state,
                above,
                staysOnTop,
                enabled);
            _ = LinuxX11Native.SendEvent(
                display,
                root,
                propagate: false,
                (nint)(LinuxX11Native.SubstructureRedirectMask |
                    LinuxX11Native.SubstructureNotifyMask),
                ref message);
            if (enabled)
            {
                _ = LinuxX11Native.RaiseWindow(display, unchecked((ulong)windowHandle));
            }

            _ = LinuxX11Native.Flush(display);
        });
    }

    internal static XEvent CreateAlwaysOnTopMessage(
        nint display,
        nint windowHandle,
        ulong stateAtom,
        ulong aboveAtom,
        ulong staysOnTopAtom,
        bool enabled)
    {
        return new XEvent
        {
            ClientMessage = new XClientMessageEvent
            {
                Type = LinuxX11Native.ClientMessage,
                Display = display,
                Window = unchecked((ulong)windowHandle),
                MessageType = stateAtom,
                Format = 32,
                Data0 = enabled
                    ? LinuxX11Native.NetWmStateAdd
                    : LinuxX11Native.NetWmStateRemove,
                Data1 = unchecked((long)aboveAtom),
                Data2 = unchecked((long)staysOnTopAtom),
                Data3 = LinuxX11Native.NetWmStateSourceApplication
            }
        };
    }

    public void FlushCompositor()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        WithDisplay(display => _ = LinuxX11Native.Sync(display, discard: false));
    }

    private static void WithDisplay(Action<nint> operation)
    {
        nint display = 0;
        try
        {
            display = LinuxX11Native.OpenDisplay(Environment.GetEnvironmentVariable("DISPLAY"));
            if (display != 0)
            {
                operation(display);
            }
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
        finally
        {
            if (display != 0)
            {
                _ = LinuxX11Native.CloseDisplay(display);
            }
        }
    }
}
