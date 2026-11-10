namespace VerboEmTela.Core.Imaging;

public sealed class Luminance8Frame
{
    private readonly ReadOnlyMemory<byte> _pixels;

    public Luminance8Frame(
        int width,
        int height,
        ReadOnlyMemory<byte> pixels)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        var expectedLength = checked(width * height);
        if (pixels.Length != expectedLength)
        {
            throw new ArgumentException(
                $"Luminance data must contain exactly {expectedLength} bytes.",
                nameof(pixels));
        }

        Width = width;
        Height = height;
        _pixels = pixels;
    }

    public int Width { get; }

    public int Height { get; }

    public ReadOnlyMemory<byte> PixelData => _pixels;
}
