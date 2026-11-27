using VerboEmTela.Core.Imaging;
using VerboEmTela.Core.Ocr;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace VerboEmTela.Core.Specs.TestData;

internal sealed class WindowsOcrRegionReader(OcrEngine engine) : IOcrRegionReader
{
    internal static WindowsOcrRegionReader? TryCreate()
    {
        var engine = OcrEngine.TryCreateFromLanguage(new Language("pt-BR"));
        return engine is not null &&
               string.Equals(engine.RecognizerLanguage.LanguageTag, "pt-BR", StringComparison.OrdinalIgnoreCase)
            ? new WindowsOcrRegionReader(engine)
            : null;
    }

    public OcrRegionRead Read(OcrFieldKind field, Luminance8Frame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        using var bitmap = new SoftwareBitmap(
            BitmapPixelFormat.Gray8,
            frame.Width,
            frame.Height,
            BitmapAlphaMode.Ignore);
        using var writer = new DataWriter();
        writer.WriteBytes(frame.PixelData.ToArray());
        bitmap.CopyFromBuffer(writer.DetachBuffer());

        var result = engine.RecognizeAsync(bitmap).AsTask().GetAwaiter().GetResult();
        var touchesBoundary = result.Lines
            .SelectMany(line => line.Words)
            .Select(word => word.BoundingRect)
            .Any(bounds => bounds.Left <= 1 || bounds.Top <= 1 ||
                           bounds.Right >= frame.Width - 1 ||
                           bounds.Bottom >= frame.Height - 1);

        return new OcrRegionRead(
            string.Join('\n', result.Lines.Select(line => line.Text)),
            touchesBoundary: touchesBoundary);
    }
}
