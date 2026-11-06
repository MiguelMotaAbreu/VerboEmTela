namespace VerboEmTela.Core.Processing;

public enum FrameProcessingStatus
{
    InvalidLayout,
    DashboardObserved,
    IgnoredUntilDashboard,
    IncompleteCall,
    CallObserved,
    CallDetected,
}
