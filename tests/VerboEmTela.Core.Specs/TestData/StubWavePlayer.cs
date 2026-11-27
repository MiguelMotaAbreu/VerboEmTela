using NAudio.Wave;

namespace VerboEmTela.Core.Specs.TestData;

internal sealed class StubWavePlayer : IWavePlayer
{
    public TaskCompletionSource StopRequested { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public IWaveProvider? Source { get; private set; }
    public bool Disposed { get; private set; }
    public bool CompleteOnPlay { get; init; }
    public Exception? InitializationError { get; init; }
    public float Volume { get; set; } = 1f;
    public PlaybackState PlaybackState { get; private set; }
    public WaveFormat OutputWaveFormat => Source!.WaveFormat;
    public event EventHandler<StoppedEventArgs>? PlaybackStopped;

    public void Init(IWaveProvider waveProvider)
    {
        if (InitializationError is { } error)
        {
            throw error;
        }

        Source = waveProvider;
    }

    public void Play()
    {
        PlaybackState = PlaybackState.Playing;
        if (CompleteOnPlay)
        {
            Complete();
        }
    }
    public void Stop() => StopRequested.TrySetResult();
    public void Pause() => PlaybackState = PlaybackState.Paused;
    public void Dispose() => Disposed = true;

    public void Complete(Exception? error = null)
    {
        PlaybackState = PlaybackState.Stopped;
        PlaybackStopped?.Invoke(this, new StoppedEventArgs(error));
    }
}
