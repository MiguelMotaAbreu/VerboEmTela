namespace VerboEmTela.Core.Processing;

public sealed record ExtractedCallText(
    string? Sector,
    string? Name,
    string? Password,
    string? Destination);
