namespace VerboEmTela.Core.Frames;

public interface IFrameCaptureGate
{
    ValueTask WaitUntilOpenAsync(
        CancellationToken cancellationToken = default);
}
