using VerboEmTela.Core.Imaging;

namespace VerboEmTela.Core.Frames;

public sealed record CapturedFrame
{
    public CapturedFrame(Rgb24Frame frame, TimeSpan observedAt)
    {
        Frame = frame ?? throw new ArgumentNullException(nameof(frame));

        if (observedAt < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(observedAt),
                "Frame time cannot be negative.");
        }

        ObservedAt = observedAt;
    }

    public Rgb24Frame Frame { get; }

    public TimeSpan ObservedAt { get; }
}
