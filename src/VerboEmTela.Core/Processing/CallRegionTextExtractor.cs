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

        var sector = Read(OcrFieldKind.Sector, regions.Sector);
        var name = Read(OcrFieldKind.Name, regions.Name);
        var password = Read(OcrFieldKind.Password, regions.Password);
        var destination = Read(
            OcrFieldKind.Destination,
            regions.Destination);

        return new ExtractedCallText(
            sector.Text,
            name.Text,
            password.Text,
            destination.Text);
    }

    private OcrRegionRead Read(
        OcrFieldKind field,
        Luminance8Frame frame)
    {
        return _reader.Read(field, frame) ??
            throw new InvalidOperationException(
                $"OCR reader returned null for field {field}.");
    }
}
