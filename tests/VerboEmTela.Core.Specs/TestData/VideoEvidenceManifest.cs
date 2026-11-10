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

internal sealed record VideoLayoutEvidenceManifest
{
    public required int SchemaVersion { get; init; }

    public required string EvidenceId { get; init; }

    public required string SourceSha256 { get; init; }

    public required int SampleIntervalMilliseconds { get; init; }

    public required int SampleCount { get; init; }

    public required int AnalysisPassCount { get; init; }

    public required int MaximumObservedTransitionVarianceMilliseconds { get; init; }

    public required double ObservedPeakWorkingSetMiB { get; init; }

    public required bool FramesPersisted { get; init; }

    public required bool TextExtracted { get; init; }

    public required IReadOnlyList<VideoLayoutEvidenceRun> Runs { get; init; }
}

internal sealed record VideoLayoutEvidenceRun
{
    public required int StartMilliseconds { get; init; }

    public required int EndMilliseconds { get; init; }

    public required int SampleCount { get; init; }

    public required string Mode { get; init; }

    public required string RejectionReason { get; init; }
}
