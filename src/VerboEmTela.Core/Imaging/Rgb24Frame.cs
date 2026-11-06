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

    internal ReadOnlySpan<byte> Pixels => _pixels.Span;
}
