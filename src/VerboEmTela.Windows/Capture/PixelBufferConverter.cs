namespace VerboEmTela.Windows.Capture;

internal static class PixelBufferConverter
{
    public static byte[] PaddedBgr24ToRgb24(
        ReadOnlySpan<byte> source,
        int width,
        int height,
        int sourceStride)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        var packedRowLength = checked(width * 3);
        if (sourceStride < packedRowLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sourceStride),
                "Source stride cannot be shorter than one packed row.");
        }

        var requiredSourceLength = checked(sourceStride * height);
        if (source.Length < requiredSourceLength)
        {
            throw new ArgumentException(
                "Source buffer is smaller than the declared image.",
                nameof(source));
        }

        var destination = new byte[checked(packedRowLength * height)];

        for (var y = 0; y < height; y++)
        {
            var sourceRow = y * sourceStride;
            var destinationRow = y * packedRowLength;

            for (var x = 0; x < width; x++)
            {
                var sourcePixel = sourceRow + (x * 3);
                var destinationPixel = destinationRow + (x * 3);

                destination[destinationPixel] = source[sourcePixel + 2];
                destination[destinationPixel + 1] = source[sourcePixel + 1];
                destination[destinationPixel + 2] = source[sourcePixel];
            }
        }

        return destination;
    }
}
