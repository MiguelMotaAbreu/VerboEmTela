namespace VerboEmTela.Core.Processing;

public enum FrameProcessingStatus
{
    LayoutTransitionObserved,
    InvalidLayout,
    DashboardObserved,
    IgnoredUntilDashboard,
    IncompleteCall,
    CallObserved,
    CallDetected,
}
