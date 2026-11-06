namespace VerboEmTela.Core.Detection;

public sealed record DetectedCall(
    string Sector,
    IdentityKind IdentityKind,
    string Identity,
    string Destination,
    TimeSpan DetectedAt);
