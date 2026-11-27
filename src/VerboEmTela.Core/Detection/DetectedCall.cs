using VerboEmTela.Core.Frames;

namespace VerboEmTela.Core.Detection;

public sealed record DetectedCall(
    string Sector,
    IdentityKind IdentityKind,
    string Identity,
    string Destination,
    TimeSpan DetectedAt)
{
    public TimeSpan VisualStartedAt { get; init; } = DetectedAt;
    public MonotonicTimestamp? VisualStartTimestamp { get; init; }
    public MonotonicTimestamp? DetectionTimestamp { get; init; }
}
