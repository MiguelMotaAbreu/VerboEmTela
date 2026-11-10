using VerboEmTela.Core.Frames;
using VerboEmTela.Core.Imaging;
using VerboEmTela.Core.Ocr;

namespace VerboEmTela.Core.Processing;

public sealed class CallRegionTextExtractor : IFrameTextExtractor
{
    private readonly CallFrameRegionExtractor _regionExtractor;
    private readonly CallRegionLuminancePreprocessor _preprocessor = new();
    private readonly IOcrRegionReader _reader;

    public CallRegionTextExtractor(
        CallRegionLayout layout,
        IOcrRegionReader reader)
    {
        _regionExtractor = new CallFrameRegionExtractor(
            layout ?? throw new ArgumentNullException(nameof(layout)));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public ExtractedCallText ExtractCall(CapturedFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        var regions = _preprocessor.Prepare(
            _regionExtractor.Extract(frame));

        var sector = ReadText(OcrFieldKind.Sector, regions.Sector);
        var name = ReadText(OcrFieldKind.Name, regions.Name);
        var password = ReadText(OcrFieldKind.Password, regions.Password);
        var destination = ReadText(
            OcrFieldKind.Destination,
            regions.Destination);

        return new ExtractedCallText(
            sector,
            name,
            password,
            destination);
    }

    private string? ReadText(
        OcrFieldKind field,
        Luminance8Frame frame)
    {
        var read = _reader.Read(field, frame) ??
            throw new InvalidOperationException(
                $"OCR reader returned null for field {field}.");

        return OcrTextNormalizer.Normalize(field, read).Text;
    }
}
