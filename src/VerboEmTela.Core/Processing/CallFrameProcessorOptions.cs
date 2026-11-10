namespace VerboEmTela.Core.Processing;

public sealed record CallFrameProcessorOptions
{
    public TimeSpan InvalidLayoutGracePeriod { get; init; } =
        TimeSpan.FromMilliseconds(800);

    internal void Validate()
    {
        if (InvalidLayoutGracePeriod <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(InvalidLayoutGracePeriod),
                "Invalid-layout grace period must be greater than zero.");
        }
    }
}
