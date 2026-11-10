using VerboEmTela.Core.Imaging;

namespace VerboEmTela.Core.Processing;

public sealed record CallRegionLayout
{
    public CallRegionLayout(
        FrameRegion sector,
        FrameRegion name,
        FrameRegion password,
        FrameRegion destination)
    {
        EnsureConfigured(sector, nameof(sector));
        EnsureConfigured(name, nameof(name));
        EnsureConfigured(password, nameof(password));
        EnsureConfigured(destination, nameof(destination));

        Sector = sector;
        Name = name;
        Password = password;
        Destination = destination;
    }

    public FrameRegion Sector { get; }

    public FrameRegion Name { get; }

    public FrameRegion Password { get; }

    public FrameRegion Destination { get; }

    private static void EnsureConfigured(FrameRegion region, string parameterName)
    {
        if (region.Width <= 0 || region.Height <= 0)
        {
            throw new ArgumentException(
                "Every call region must have positive dimensions.",
                parameterName);
        }
    }
}
