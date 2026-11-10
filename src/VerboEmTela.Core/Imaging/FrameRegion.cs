namespace VerboEmTela.Core.Imaging;

public readonly record struct FrameRegion
{
    public FrameRegion(int left, int top, int width, int height)
    {
        if (left < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(left));
        }

        if (top < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(top));
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        _ = checked(left + width);
        _ = checked(top + height);

        Left = left;
        Top = top;
        Width = width;
        Height = height;
    }

    public int Left { get; }

    public int Top { get; }

    public int Width { get; }

    public int Height { get; }

    public int Right => checked(Left + Width);

    public int Bottom => checked(Top + Height);
}
