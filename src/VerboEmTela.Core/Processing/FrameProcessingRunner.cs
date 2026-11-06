using System.Runtime.CompilerServices;
using VerboEmTela.Core.Frames;

namespace VerboEmTela.Core.Processing;

public sealed class FrameProcessingRunner
{
    private readonly CallFrameProcessor _processor;
    private int _isRunning;

    public FrameProcessingRunner(CallFrameProcessor processor)
    {
        _processor = processor ?? throw new ArgumentNullException(nameof(processor));
    }

    public async IAsyncEnumerable<FrameProcessingResult> RunAsync(
        IFrameSource source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (Interlocked.Exchange(ref _isRunning, 1) != 0)
        {
            throw new InvalidOperationException(
                "This frame-processing runner is already active.");
        }

        _processor.Reset();

        try
        {
            await foreach (var capturedFrame in source
                .ReadFramesAsync(cancellationToken)
                .WithCancellation(cancellationToken)
                .ConfigureAwait(false))
            {
                yield return _processor.Process(
                    capturedFrame.Frame,
                    capturedFrame.ObservedAt);
            }
        }
        finally
        {
            _processor.Reset();
            Volatile.Write(ref _isRunning, 0);
        }
    }
}
