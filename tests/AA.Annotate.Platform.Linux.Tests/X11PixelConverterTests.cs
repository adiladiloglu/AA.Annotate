using AA.Annotate.Platform.Linux;

namespace AA.Annotate.Platform.Linux.Tests;

public sealed class X11PixelConverterTests
{
    [Fact]
    public void ConvertsLittleEndianBgrx8888WithPaddedStride()
    {
        var image = new X11ImageData(
            Width: 2,
            Height: 1,
            XOffset: 0,
            ByteOrder: LinuxX11Native.LsbFirst,
            BytesPerLine: 12,
            BitsPerPixel: 32,
            RedMask: 0x00FF0000,
            GreenMask: 0x0000FF00,
            BlueMask: 0x000000FF,
            Bytes:
            [
                0x33, 0x22, 0x11, 0x00,
                0x66, 0x55, 0x44, 0x00,
                0xAA, 0xBB, 0xCC, 0xDD
            ]);

        var pixels = X11PixelConverter.ConvertToBgra(image);

        Assert.Equal(
            new byte[]
            {
                0x33, 0x22, 0x11, 0xFF,
                0x66, 0x55, 0x44, 0xFF
            },
            pixels);
    }

    [Fact]
    public void ConvertsBigEndianRgb888()
    {
        var image = new X11ImageData(
            Width: 2,
            Height: 1,
            XOffset: 0,
            ByteOrder: 1,
            BytesPerLine: 6,
            BitsPerPixel: 24,
            RedMask: 0xFF0000,
            GreenMask: 0x00FF00,
            BlueMask: 0x0000FF,
            Bytes: [0x11, 0x22, 0x33, 0xAA, 0xBB, 0xCC]);

        var pixels = X11PixelConverter.ConvertToBgra(image);

        Assert.Equal(
            new byte[]
            {
                0x33, 0x22, 0x11, 0xFF,
                0xCC, 0xBB, 0xAA, 0xFF
            },
            pixels);
    }

    [Fact]
    public void ScalesRgb565ChannelsToEightBits()
    {
        var image = new X11ImageData(
            Width: 3,
            Height: 1,
            XOffset: 0,
            ByteOrder: LinuxX11Native.LsbFirst,
            BytesPerLine: 6,
            BitsPerPixel: 16,
            RedMask: 0xF800,
            GreenMask: 0x07E0,
            BlueMask: 0x001F,
            Bytes: [0x00, 0xF8, 0xE0, 0x07, 0x1F, 0x00]);

        var pixels = X11PixelConverter.ConvertToBgra(image);

        Assert.Equal(
            new byte[]
            {
                0x00, 0x00, 0xFF, 0xFF,
                0x00, 0xFF, 0x00, 0xFF,
                0xFF, 0x00, 0x00, 0xFF
            },
            pixels);
    }

    [Fact]
    public void HonorsPixelXOffset()
    {
        var image = new X11ImageData(
            Width: 1,
            Height: 1,
            XOffset: 1,
            ByteOrder: LinuxX11Native.LsbFirst,
            BytesPerLine: 8,
            BitsPerPixel: 32,
            RedMask: 0x00FF0000,
            GreenMask: 0x0000FF00,
            BlueMask: 0x000000FF,
            Bytes: [0xEE, 0xEE, 0xEE, 0xEE, 0x30, 0x20, 0x10, 0x00]);

        var pixels = X11PixelConverter.ConvertToBgra(image);

        Assert.Equal(new byte[] { 0x30, 0x20, 0x10, 0xFF }, pixels);
    }

    [Theory]
    [InlineData(7, 0, 4)]
    [InlineData(32, -1, 5)]
    [InlineData(32, 0, 3)]
    public void RejectsUnsupportedPackingOrShortStride(
        int bitsPerPixel,
        int xOffset,
        int bytesPerLine)
    {
        var image = new X11ImageData(
            Width: 1,
            Height: 1,
            XOffset: xOffset,
            ByteOrder: LinuxX11Native.LsbFirst,
            BytesPerLine: bytesPerLine,
            BitsPerPixel: bitsPerPixel,
            RedMask: 0xFF0000,
            GreenMask: 0x00FF00,
            BlueMask: 0x0000FF,
            Bytes: new byte[5]);

        Assert.Throws<InvalidDataException>(() => X11PixelConverter.ConvertToBgra(image));
    }
}
