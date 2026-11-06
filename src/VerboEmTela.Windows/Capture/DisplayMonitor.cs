namespace VerboEmTela.Windows.Capture;

public sealed record DisplayMonitor
{
    public DisplayMonitor(string deviceName, ScreenRegion bounds, bool isPrimary)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            throw new ArgumentException(
                "A display monitor must have a device name.",
                nameof(deviceName));
        }

        DeviceName = deviceName.Trim();
        Bounds = bounds;
        IsPrimary = isPrimary;
    }

    public string DeviceName { get; }

    public ScreenRegion Bounds { get; }

    public bool IsPrimary { get; }
}
