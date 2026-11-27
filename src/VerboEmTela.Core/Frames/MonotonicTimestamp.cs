namespace VerboEmTela.Core.Frames;

public readonly record struct MonotonicTimestamp(TimeProvider Clock, long Value);
