namespace VerboEmTela.Core.Specs.TestData;

internal sealed record VideoEvidenceManifest
{
    public required int SchemaVersion { get; init; }

    public required string EvidenceId { get; init; }

    public required string Sha256 { get; init; }

    public required long SizeBytes { get; init; }

    public required double DurationSeconds { get; init; }

    public required VideoTrackEvidence Video { get; init; }

    public required AudioTrackEvidence Audio { get; init; }
}

internal sealed record VideoTrackEvidence
{
    public required int Width { get; init; }

    public required int Height { get; init; }

    public required double FramesPerSecond { get; init; }

    public required int DataRateKbps { get; init; }

    public required string CodecIdentifier { get; init; }
}

internal sealed record AudioTrackEvidence
{
    public required bool Present { get; init; }

    public required int BitRateKbps { get; init; }

    public required bool CodecIdentified { get; init; }
}
