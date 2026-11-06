namespace VerboEmTela.Windows.Capture;

public interface IDisplayMonitorProvider
{
    IReadOnlyList<DisplayMonitor> GetMonitors();
}
