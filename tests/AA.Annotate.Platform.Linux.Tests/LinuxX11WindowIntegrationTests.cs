using System.Runtime.InteropServices;
using AA.Annotate.Platform.Linux;

namespace AA.Annotate.Platform.Linux.Tests;

public sealed class LinuxX11WindowIntegrationTests
{
    [Theory]
    [InlineData(true, LinuxX11Native.NetWmStateAdd)]
    [InlineData(false, LinuxX11Native.NetWmStateRemove)]
    public void AlwaysOnTopMessageUsesEwmhMappedWindowProtocol(bool enabled, long expectedAction)
    {
        var message = LinuxX11WindowIntegration.CreateAlwaysOnTopMessage(
            display: (nint)11,
            windowHandle: (nint)22,
            stateAtom: 33,
            aboveAtom: 44,
            staysOnTopAtom: 55,
            enabled);

        Assert.Equal(LinuxX11Native.ClientMessage, message.ClientMessage.Type);
        Assert.Equal((nint)11, message.ClientMessage.Display);
        Assert.Equal(22UL, message.ClientMessage.Window);
        Assert.Equal(33UL, message.ClientMessage.MessageType);
        Assert.Equal(32, message.ClientMessage.Format);
        Assert.Equal(expectedAction, message.ClientMessage.Data0);
        Assert.Equal(44, message.ClientMessage.Data1);
        Assert.Equal(55, message.ClientMessage.Data2);
        Assert.Equal(LinuxX11Native.NetWmStateSourceApplication, message.ClientMessage.Data3);
    }

    [Fact]
    public void ClientMessageInteropLayoutMatchesXlibOn64Bit()
    {
        if (nint.Size != 8)
        {
            return;
        }

        Assert.Equal(96, Marshal.SizeOf<XClientMessageEvent>());
        Assert.Equal(192, Marshal.SizeOf<XEvent>());
    }
}
