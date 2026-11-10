using VerboEmTela.Core.Imaging;

namespace VerboEmTela.Ocr.Tesseract;

internal static class GrayscaleBmpEncoder
{
    private const int FileHeaderSize = 14;
    private const int BitmapInfoHeaderSize = 40;
    private const int PaletteSize = 256 * 4;
    private const int PixelOffset = FileHeaderSize + BitmapInfoHeaderSize + PaletteSize;

    public static byte[] Encode(Luminance8Frame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        var stride = checked((frame.Width + 3) & ~3);
        var pixelDataSize = checked(stride * frame.Height);
        var fileSize = checked(PixelOffset + pixelDataSize);
        var bitmap = new byte[fileSize];

        bitmap[0] = (byte)'B';
        bitmap[1] = (byte)'M';
        WriteInt32(bitmap, 2, fileSize);
        WriteInt32(bitmap, 10, PixelOffset);
        WriteInt32(bitmap, 14, BitmapInfoHeaderSize);
        WriteInt32(bitmap, 18, frame.Width);
        WriteInt32(bitmap, 22, frame.Height);
        WriteInt16(bitmap, 26, 1);
        WriteInt16(bitmap, 28, 8);
        WriteInt32(bitmap, 34, pixelDataSize);
        WriteInt32(bitmap, 38, 3_780);
        WriteInt32(bitmap, 42, 3_780);
        WriteInt32(bitmap, 46, 256);

        for (var value = 0; value < 256; value++)
        {
            var paletteIndex = FileHeaderSize + BitmapInfoHeaderSize + (value * 4);
            bitmap[paletteIndex] = (byte)value;
            bitmap[paletteIndex + 1] = (byte)value;
            bitmap[paletteIndex + 2] = (byte)value;
        }

        var pixels = frame.PixelData.Span;
        for (var sourceRow = 0; sourceRow < frame.Height; sourceRow++)
        {
            var destinationRow = frame.Height - sourceRow - 1;
            pixels
                .Slice(sourceRow * frame.Width, frame.Width)
                .CopyTo(bitmap.AsSpan(
                    PixelOffset + (destinationRow * stride),
                    frame.Width));
        }

        return bitmap;
    }

    private static void WriteInt16(byte[] target, int offset, int value)
    {
        target[offset] = (byte)value;
        target[offset + 1] = (byte)(value >> 8);
    }

    private static void WriteInt32(byte[] target, int offset, int value)
    {
        target[offset] = (byte)value;
        target[offset + 1] = (byte)(value >> 8);
        target[offset + 2] = (byte)(value >> 16);
        target[offset + 3] = (byte)(value >> 24);
    }
}
