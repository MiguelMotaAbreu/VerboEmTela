namespace VerboEmTela.Core.Layout;

public sealed record PanelLayoutRecognizerOptions
{
    public int RequiredWidth { get; init; } = 1920;

    public int RequiredHeight { get; init; } = 1080;

    public int DashboardBandCount { get; init; } = 3;

    public double MinimumGreenRowCoverage { get; init; } = 0.70;

    public double MinimumBandHeightRatio { get; init; } = 0.008;

    public double MaximumGapInsideBandRatio { get; init; } = 0.002;

    public double MinimumLightPixelRatio { get; init; } = 0.45;

    public double MinimumFirstBandTopRatio { get; init; } = 0.03;

    public double MaximumFirstBandTopRatio { get; init; } = 0.55;

    internal void Validate()
    {
        if (RequiredWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(RequiredWidth));
        }

        if (RequiredHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(RequiredHeight));
        }

        if (DashboardBandCount < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(DashboardBandCount));
        }

        ValidateRatio(MinimumGreenRowCoverage, nameof(MinimumGreenRowCoverage), allowZero: false);
        ValidateRatio(MinimumBandHeightRatio, nameof(MinimumBandHeightRatio), allowZero: false);
        ValidateRatio(MaximumGapInsideBandRatio, nameof(MaximumGapInsideBandRatio), allowZero: true);
        ValidateRatio(MinimumLightPixelRatio, nameof(MinimumLightPixelRatio), allowZero: false);
        ValidateRatio(MinimumFirstBandTopRatio, nameof(MinimumFirstBandTopRatio), allowZero: true);
        ValidateRatio(MaximumFirstBandTopRatio, nameof(MaximumFirstBandTopRatio), allowZero: false);

        if (MinimumFirstBandTopRatio >= MaximumFirstBandTopRatio)
        {
            throw new ArgumentException("The first-band region is empty or inverted.");
        }
    }

    private static void ValidateRatio(double value, string name, bool allowZero)
    {
        var minimum = allowZero ? 0 : double.Epsilon;
        if (value < minimum || value > 1)
        {
            throw new ArgumentOutOfRangeException(name, "Ratio must be between zero and one.");
        }
    }
}
