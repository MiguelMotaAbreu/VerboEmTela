namespace VerboEmTela.Windows.Capture;

public sealed record ScreenFrameSourceOptions
{
    public required ScreenRegion Region { get; init; }

    public double FramesPerSecond { get; init; } = 5;

    internal TimeSpan ValidateAndGetInterval()
    {
        if (double.IsNaN(FramesPerSecond) ||
            double.IsInfinity(FramesPerSecond) ||
            FramesPerSecond <= 0 ||
            FramesPerSecond > 60)
        {
            throw new ArgumentOutOfRangeException(
                nameof(FramesPerSecond),
                "Frame rate must be greater than zero and at most 60 FPS.");
        }

        return TimeSpan.FromSeconds(1 / FramesPerSecond);
    }
}
