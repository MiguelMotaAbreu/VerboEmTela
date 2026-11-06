namespace VerboEmTela.Core.Layout;

public readonly record struct HorizontalBand(
    int Top,
    int BottomExclusive,
    double AverageGreenCoverage)
{
    public int Height => BottomExclusive - Top;
}
