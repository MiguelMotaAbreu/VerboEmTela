namespace VerboEmTela.Core.Detection;

public sealed record DetectedCall(
    string Sector,
    string Identity,
    string Destination,
    TimeSpan DetectedAt);
