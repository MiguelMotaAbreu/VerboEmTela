namespace VerboEmTela.Windows.Capture;

public sealed record PanelMonitorSelectionResult
{
    private PanelMonitorSelectionResult(
        PanelMonitorSelectionStatus status,
        DisplayMonitor? monitor,
        int scannedMonitorCount,
        int matchingMonitorCount)
    {
        Status = status;
        Monitor = monitor;
        ScannedMonitorCount = scannedMonitorCount;
        MatchingMonitorCount = matchingMonitorCount;
    }

    public PanelMonitorSelectionStatus Status { get; }

    public DisplayMonitor? Monitor { get; }

    public int ScannedMonitorCount { get; }

    public int MatchingMonitorCount { get; }

    internal static PanelMonitorSelectionResult NoMonitors()
    {
        return new PanelMonitorSelectionResult(
            PanelMonitorSelectionStatus.NoMonitorsAvailable,
            null,
            0,
            0);
    }

    internal static PanelMonitorSelectionResult NoPanel(int scannedMonitorCount)
    {
        return new PanelMonitorSelectionResult(
            PanelMonitorSelectionStatus.NoPanelFound,
            null,
            scannedMonitorCount,
            0);
    }

    internal static PanelMonitorSelectionResult MultiplePanels(
        int scannedMonitorCount,
        int matchingMonitorCount)
    {
        return new PanelMonitorSelectionResult(
            PanelMonitorSelectionStatus.MultiplePanelsFound,
            null,
            scannedMonitorCount,
            matchingMonitorCount);
    }

    internal static PanelMonitorSelectionResult Selected(
        DisplayMonitor monitor,
        int scannedMonitorCount)
    {
        return new PanelMonitorSelectionResult(
            PanelMonitorSelectionStatus.Selected,
            monitor,
            scannedMonitorCount,
            1);
    }
}
