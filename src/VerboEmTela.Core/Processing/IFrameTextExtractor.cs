using VerboEmTela.Core.Imaging;

namespace VerboEmTela.Core.Processing;

public interface IFrameTextExtractor
{
    ExtractedCallText ExtractCall(Rgb24Frame frame);
}
