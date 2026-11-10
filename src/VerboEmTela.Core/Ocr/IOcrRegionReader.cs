using VerboEmTela.Core.Imaging;

namespace VerboEmTela.Core.Ocr;

public interface IOcrRegionReader
{
    OcrRegionRead Read(
        OcrFieldKind field,
        Luminance8Frame frame);
}
