using VerboEmTela.Core.Detection;
using VerboEmTela.Core.Layout;

namespace VerboEmTela.Core.Processing;

public sealed record FrameProcessingResult(
    PanelMode PanelMode,
    FrameProcessingStatus Status,
    LayoutRejectionReason LayoutRejectionReason,
    DetectedCall? DetectedCall);
