using VerboEmTela.Core.Frames;
using VerboEmTela.Core.Imaging;

namespace VerboEmTela.Core.Processing;

public sealed class CallFrameRegionExtractor
{
    private readonly CallRegionLayout _layout;

    public CallFrameRegionExtractor(CallRegionLayout layout)
    {
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
    }

    public CallFrameRegions Extract(CapturedFrame capturedFrame)
    {
        ArgumentNullException.ThrowIfNull(capturedFrame);

        var frame = capturedFrame.Frame;
        EnsureContained(frame, _layout.Sector, nameof(_layout.Sector));
        EnsureContained(frame, _layout.Name, nameof(_layout.Name));
        EnsureContained(frame, _layout.Password, nameof(_layout.Password));
        EnsureContained(frame, _layout.Destination, nameof(_layout.Destination));

        return new CallFrameRegions(
            capturedFrame.ObservedAt,
            frame.Crop(_layout.Sector),
            frame.Crop(_layout.Name),
            frame.Crop(_layout.Password),
            frame.Crop(_layout.Destination));
    }

    private static void EnsureContained(
        Rgb24Frame frame,
        FrameRegion region,
        string regionName)
    {
        if (!frame.Contains(region))
        {
            throw new ArgumentException(
                $"Configured call region {regionName} is outside the captured frame.",
                nameof(region));
        }
    }
}
