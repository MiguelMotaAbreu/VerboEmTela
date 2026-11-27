using VerboEmTela.Core.Speech;

namespace VerboEmTela.Core.Specs.TestData;

internal sealed class ManualTimestampProvider(bool suspendTimers = false) : TimeProvider
{
    private long _timestamp;
    private ITimer? _suspendedTimer;

    public TimeSpan? LastTimerDelay { get; private set; }
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    public override long GetTimestamp() => _timestamp;
    public override DateTimeOffset GetUtcNow() => throw new InvalidOperationException("Use the monotonic clock.");

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        LastTimerDelay = dueTime;
        var timer = base.CreateTimer(callback, state, suspendTimers ? Timeout.InfiniteTimeSpan : dueTime, period);
        if (suspendTimers)
        {
            _suspendedTimer = timer;
        }

        return timer;
    }

    public void FireSuspendedTimer() =>
        (_suspendedTimer ?? throw new InvalidOperationException("No suspended timer was scheduled."))
            .Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);

    public void Advance(TimeSpan interval) => _timestamp += interval.Ticks;
}

internal sealed class StubSpeechEngine(Func<SynthesizedSpeech> synthesize) : ISpeechEngine
{
    public string? LastText { get; private set; }

    public SynthesizedSpeech Synthesize(string text, float speed = 1f)
    {
        LastText = text;
        return synthesize();
    }
}

internal sealed class StubAudioOutput(Func<CancellationToken, Task>? play = null) : IAudioOutput
{
    public int PlayCount { get; private set; }
    public SynthesizedSpeech? LastSpeech { get; private set; }

    public Task PlayAsync(SynthesizedSpeech speech, CancellationToken cancellationToken = default)
    {
        PlayCount++;
        LastSpeech = speech;
        return play?.Invoke(cancellationToken) ?? Task.CompletedTask;
    }
}
