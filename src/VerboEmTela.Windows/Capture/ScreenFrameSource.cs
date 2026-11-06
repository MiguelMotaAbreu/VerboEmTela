using System.Runtime.CompilerServices;
using VerboEmTela.Core.Frames;

namespace VerboEmTela.Windows.Capture;

public sealed class ScreenFrameSource : IFrameSource
{
    private readonly IScreenPixelCapture _capture;
    private readonly ScreenFrameSourceOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _frameInterval;

    public ScreenFrameSource(
        IScreenPixelCapture capture,
        ScreenFrameSourceOptions options,
        TimeProvider? timeProvider = null)
    {
        _capture = capture ?? throw new ArgumentNullException(nameof(capture));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _frameInterval = _options.ValidateAndGetInterval();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async IAsyncEnumerable<CapturedFrame> ReadFramesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sourceStartedAt = _timeProvider.GetTimestamp();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var iterationStartedAt = _timeProvider.GetTimestamp();
            var frame = _capture.Capture(_options.Region);
            var capturedAt = _timeProvider.GetTimestamp();

            yield return new CapturedFrame(
                frame,
                _timeProvider.GetElapsedTime(sourceStartedAt, capturedAt));

            var iterationElapsed = _timeProvider.GetElapsedTime(
                iterationStartedAt,
                _timeProvider.GetTimestamp());
            var remainingDelay = _frameInterval - iterationElapsed;

            if (remainingDelay > TimeSpan.Zero)
            {
                await Task.Delay(
                    remainingDelay,
                    _timeProvider,
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
