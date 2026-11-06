namespace VerboEmTela.Core.Frames;

public interface IFrameSource
{
    IAsyncEnumerable<CapturedFrame> ReadFramesAsync(
        CancellationToken cancellationToken = default);
}
