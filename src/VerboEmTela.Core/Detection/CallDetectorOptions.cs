namespace VerboEmTela.Core.Detection;

public sealed record CallDetectorOptions
{
    public int MinimumStableObservations { get; init; } = 3;

    public TimeSpan MinimumStableDuration { get; init; } = TimeSpan.FromMilliseconds(600);

    internal void Validate()
    {
        if (MinimumStableObservations < 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MinimumStableObservations),
                "At least two observations are required to establish stability.");
        }

        if (MinimumStableDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MinimumStableDuration),
                "Stable duration must be greater than zero.");
        }
    }
}
