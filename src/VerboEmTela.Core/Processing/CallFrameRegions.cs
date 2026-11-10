using VerboEmTela.Core.Imaging;

namespace VerboEmTela.Core.Processing;

public sealed record CallFrameRegions
{
    internal CallFrameRegions(
        TimeSpan observedAt,
        Rgb24Frame sector,
        Rgb24Frame name,
        Rgb24Frame password,
        Rgb24Frame destination)
    {
        if (observedAt < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(observedAt));
        }

        ObservedAt = observedAt;
        Sector = sector ?? throw new ArgumentNullException(nameof(sector));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Password = password ?? throw new ArgumentNullException(nameof(password));
        Destination = destination ?? throw new ArgumentNullException(nameof(destination));
    }

    public TimeSpan ObservedAt { get; }

    public Rgb24Frame Sector { get; }

    public Rgb24Frame Name { get; }

    public Rgb24Frame Password { get; }

    public Rgb24Frame Destination { get; }
}
