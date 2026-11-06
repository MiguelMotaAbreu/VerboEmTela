namespace VerboEmTela.Core.Layout;

public enum LayoutRejectionReason
{
    None,
    UnexpectedDimensions,
    InsufficientLightBackground,
    UnexpectedGreenBandCount,
    FirstBandOutsideExpectedRegion,
}
