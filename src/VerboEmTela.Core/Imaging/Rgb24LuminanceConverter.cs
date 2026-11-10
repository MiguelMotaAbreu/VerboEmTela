namespace VerboEmTela.Core.Imaging;

public static class Rgb24LuminanceConverter
{
    public static Luminance8Frame Convert(Rgb24Frame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        var source = frame.Pixels;
        var destination = new byte[checked(frame.Width * frame.Height)];

        for (var pixel = 0; pixel < destination.Length; pixel++)
        {
            var sourceIndex = pixel * 3;
            var red = source[sourceIndex];
            var green = source[sourceIndex + 1];
            var blue = source[sourceIndex + 2];

            destination[pixel] = (byte)(
                ((77 * red) + (150 * green) + (29 * blue) + 128) >> 8);
        }

        return new Luminance8Frame(
            frame.Width,
            frame.Height,
            destination);
    }
}
