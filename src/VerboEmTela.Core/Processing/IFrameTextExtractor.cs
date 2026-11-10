using VerboEmTela.Core.Frames;

namespace VerboEmTela.Core.Processing;

public interface IFrameTextExtractor
{
    ExtractedCallText ExtractCall(CapturedFrame frame);
}
