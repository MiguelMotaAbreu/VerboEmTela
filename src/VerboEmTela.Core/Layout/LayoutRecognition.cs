using VerboEmTela.Core.Detection;

namespace VerboEmTela.Core.Layout;

public sealed record LayoutRecognition(
    PanelMode Mode,
    IReadOnlyList<HorizontalBand> GreenBands,
    LayoutRejectionReason RejectionReason)
{
    public bool IsValidPanel => Mode is not PanelMode.Invalid;
}
