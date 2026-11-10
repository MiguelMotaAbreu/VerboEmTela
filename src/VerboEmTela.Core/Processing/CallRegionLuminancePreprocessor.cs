using VerboEmTela.Core.Imaging;

namespace VerboEmTela.Core.Processing;

public sealed class CallRegionLuminancePreprocessor
{
    public CallLuminanceRegions Prepare(CallFrameRegions regions)
    {
        ArgumentNullException.ThrowIfNull(regions);

        return new CallLuminanceRegions(
            regions.ObservedAt,
            Rgb24LuminanceConverter.Convert(regions.Sector),
            Rgb24LuminanceConverter.Convert(regions.Name),
            Rgb24LuminanceConverter.Convert(regions.Password),
            Rgb24LuminanceConverter.Convert(regions.Destination));
    }
}
