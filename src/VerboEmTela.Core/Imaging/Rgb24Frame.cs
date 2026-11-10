namespace VerboEmTela.Core.Imaging;

public sealed class Rgb24Frame
{
    private const int BytesPerPixel = 3;
    private readonly ReadOnlyMemory<byte> _pixels;

    public Rgb24Frame(int width, int height, ReadOnlyMemory<byte> pixels)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than zero.");
        }

        var expectedLength = checked(width * height * BytesPerPixel);
        if (pixels.Length != expectedLength)
        {
            throw new ArgumentException(
                $"RGB24 data must contain exactly {expectedLength} bytes.",
                nameof(pixels));
        }

        Width = width;
        Height = height;
        _pixels = pixels;
    }

    public int Width { get; }

    public int Height { get; }

    public ReadOnlyMemory<byte> PixelData => _pixels;

    internal ReadOnlySpan<byte> Pixels => _pixels.Span;

    public Rgb24Frame Crop(FrameRegion region)
    {
        if (region.Width <= 0 || region.Height <= 0)
        {
            throw new ArgumentException(
                "Frame region must have positive dimensions.",
                nameof(region));
        }

        if (region.Left < 0 ||
            region.Top < 0 ||
            region.Right > Width ||
            region.Bottom > Height)
        {
            throw new ArgumentException(
                "Frame region must be fully contained in the source frame.",
                nameof(region));
        }

        var source = _pixels.Span;
        var rowLength = checked(region.Width * BytesPerPixel);
        var croppedPixels = new byte[checked(rowLength * region.Height)];

        for (var row = 0; row < region.Height; row++)
        {
            var sourceStart = checked(
                (((region.Top + row) * Width) + region.Left) *
                BytesPerPixel);
            var destinationStart = row * rowLength;

            source
                .Slice(sourceStart, rowLength)
                .CopyTo(croppedPixels.AsSpan(destinationStart, rowLength));
        }

        return new Rgb24Frame(region.Width, region.Height, croppedPixels);
    }
}
