using VerboEmTela.Core.Imaging;

namespace VerboEmTela.Core.Processing;

public sealed record CallLuminanceRegions
{
    internal CallLuminanceRegions(
        TimeSpan observedAt,
        Luminance8Frame sector,
        Luminance8Frame name,
        Luminance8Frame password,
        Luminance8Frame destination)
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

    public Luminance8Frame Sector { get; }

    public Luminance8Frame Name { get; }

    public Luminance8Frame Password { get; }

    public Luminance8Frame Destination { get; }
}
