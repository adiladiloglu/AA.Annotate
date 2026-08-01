namespace AA.Annotate.Platform.Linux;

internal sealed record X11ImageData(
    int Width,
    int Height,
    int XOffset,
    int ByteOrder,
    int BytesPerLine,
    int BitsPerPixel,
    ulong RedMask,
    ulong GreenMask,
    ulong BlueMask,
    byte[] Bytes);

internal static class X11PixelConverter
{
    internal static byte[] ConvertToBgra(X11ImageData image)
    {
        ArgumentNullException.ThrowIfNull(image);

        if (image.Width <= 0 || image.Height <= 0)
        {
            throw new InvalidDataException("The X11 image has invalid dimensions.");
        }

        if (image.BitsPerPixel is < 8 or > 32 || image.BitsPerPixel % 8 != 0)
        {
            throw new InvalidDataException(
                $"The X11 image uses unsupported {image.BitsPerPixel}-bit packed pixels.");
        }

        if (image.XOffset < 0)
        {
            throw new InvalidDataException("The X11 image uses a negative pixel offset.");
        }

        if (image.ByteOrder is not LinuxX11Native.LsbFirst and not 1)
        {
            throw new InvalidDataException($"The X11 image has unknown byte order {image.ByteOrder}.");
        }

        if (image.RedMask == 0 || image.GreenMask == 0 || image.BlueMask == 0)
        {
            throw new InvalidDataException("The X11 image does not define RGB color masks.");
        }

        if ((image.RedMask & image.GreenMask) != 0
            || (image.RedMask & image.BlueMask) != 0
            || (image.GreenMask & image.BlueMask) != 0
            || !IsContiguous(image.RedMask)
            || !IsContiguous(image.GreenMask)
            || !IsContiguous(image.BlueMask))
        {
            throw new InvalidDataException("The X11 image defines invalid or overlapping RGB color masks.");
        }

        var pixelBytes = image.BitsPerPixel / 8;
        var rowStart = checked(image.XOffset * pixelBytes);
        var requiredRowBytes = checked(rowStart + (image.Width * pixelBytes));
        if (image.BytesPerLine < requiredRowBytes)
        {
            throw new InvalidDataException("The X11 image stride is smaller than its pixel data.");
        }

        var requiredLength = checked(image.BytesPerLine * image.Height);
        if (image.Bytes.Length < requiredLength)
        {
            throw new InvalidDataException("The X11 image buffer is shorter than its declared stride.");
        }

        var destination = new byte[checked(image.Width * image.Height * 4)];
        var destinationOffset = 0;

        for (var y = 0; y < image.Height; y++)
        {
            var sourceOffset = checked((y * image.BytesPerLine) + rowStart);
            for (var x = 0; x < image.Width; x++)
            {
                var pixel = ReadPixel(
                    image.Bytes,
                    sourceOffset + (x * pixelBytes),
                    pixelBytes,
                    image.ByteOrder);

                destination[destinationOffset] = ScaleChannel(pixel, image.BlueMask);
                destination[destinationOffset + 1] = ScaleChannel(pixel, image.GreenMask);
                destination[destinationOffset + 2] = ScaleChannel(pixel, image.RedMask);
                destination[destinationOffset + 3] = byte.MaxValue;
                destinationOffset += 4;
            }
        }

        return destination;
    }

    private static ulong ReadPixel(byte[] source, int offset, int byteCount, int byteOrder)
    {
        ulong pixel = 0;
        if (byteOrder == LinuxX11Native.LsbFirst)
        {
            for (var index = 0; index < byteCount; index++)
            {
                pixel |= (ulong)source[offset + index] << (index * 8);
            }
        }
        else
        {
            for (var index = 0; index < byteCount; index++)
            {
                pixel = (pixel << 8) | source[offset + index];
            }
        }

        return pixel;
    }

    private static byte ScaleChannel(ulong pixel, ulong mask)
    {
        var shift = CountTrailingZeroBits(mask);
        var maximum = mask >> shift;
        var value = (pixel & mask) >> shift;
        return checked((byte)((value * byte.MaxValue + (maximum / 2)) / maximum));
    }

    private static int CountTrailingZeroBits(ulong value)
    {
        var count = 0;
        while ((value & 1) == 0)
        {
            value >>= 1;
            count++;
        }

        return count;
    }

    private static bool IsContiguous(ulong mask)
    {
        var shifted = mask >> CountTrailingZeroBits(mask);
        return (shifted & (shifted + 1)) == 0;
    }
}
