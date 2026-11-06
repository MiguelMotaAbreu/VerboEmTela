using VerboEmTela.Core.Detection;

namespace VerboEmTela.Core.Layout;

public sealed record LayoutMonitoringEvent(
    TimeSpan ObservedAt,
    PanelMode Mode,
    LayoutRejectionReason RejectionReason);
