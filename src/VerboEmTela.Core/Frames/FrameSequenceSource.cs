using System.Runtime.CompilerServices;

namespace VerboEmTela.Core.Frames;

public sealed class FrameSequenceSource : IFrameSource
{
    private readonly IReadOnlyList<CapturedFrame> _frames;

    public FrameSequenceSource(IEnumerable<CapturedFrame> frames)
    {
        ArgumentNullException.ThrowIfNull(frames);

        var materializedFrames = frames.ToArray();
        ValidateChronologicalOrder(materializedFrames);
        _frames = materializedFrames;
    }

    public async IAsyncEnumerable<CapturedFrame> ReadFramesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var frame in _frames)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return frame;
        }
    }

    private static void ValidateChronologicalOrder(
        IReadOnlyList<CapturedFrame> frames)
    {
        TimeSpan? previousTime = null;

        for (var index = 0; index < frames.Count; index++)
        {
            var frame = frames[index] ?? throw new ArgumentException(
                "Frame sequence cannot contain null items.",
                nameof(frames));

            if (previousTime is not null && frame.ObservedAt < previousTime)
            {
                throw new ArgumentException(
                    "Frame sequence must be ordered chronologically.",
                    nameof(frames));
            }

            previousTime = frame.ObservedAt;
        }
    }
}
