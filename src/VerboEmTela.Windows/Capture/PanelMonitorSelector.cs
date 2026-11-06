using VerboEmTela.Core.Layout;

namespace VerboEmTela.Windows.Capture;

public sealed class PanelMonitorSelector
{
    private readonly IDisplayMonitorProvider _monitorProvider;
    private readonly IScreenPixelCapture _capture;
    private readonly IPanelLayoutRecognizer _layoutRecognizer;

    public PanelMonitorSelector(
        IDisplayMonitorProvider monitorProvider,
        IScreenPixelCapture capture,
        IPanelLayoutRecognizer layoutRecognizer)
    {
        _monitorProvider = monitorProvider ??
            throw new ArgumentNullException(nameof(monitorProvider));
        _capture = capture ?? throw new ArgumentNullException(nameof(capture));
        _layoutRecognizer = layoutRecognizer ??
            throw new ArgumentNullException(nameof(layoutRecognizer));
    }

    public PanelMonitorSelectionResult Select()
    {
        var monitors = _monitorProvider.GetMonitors() ??
            throw new InvalidOperationException("The monitor provider returned null.");

        if (monitors.Count == 0)
        {
            return PanelMonitorSelectionResult.NoMonitors();
        }

        DisplayMonitor? selectedMonitor = null;
        var matchingMonitorCount = 0;

        foreach (var monitor in monitors)
        {
            ArgumentNullException.ThrowIfNull(monitor);

            var frame = _capture.Capture(monitor.Bounds);
            var recognition = _layoutRecognizer.Recognize(frame);

            if (!recognition.IsValidPanel)
            {
                continue;
            }

            matchingMonitorCount++;
            selectedMonitor ??= monitor;
        }

        return matchingMonitorCount switch
        {
            0 => PanelMonitorSelectionResult.NoPanel(monitors.Count),
            1 => PanelMonitorSelectionResult.Selected(selectedMonitor!, monitors.Count),
            _ => PanelMonitorSelectionResult.MultiplePanels(
                monitors.Count,
                matchingMonitorCount),
        };
    }
}
