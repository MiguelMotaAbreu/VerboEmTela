using NAudio;
using NAudio.Wave;
using VerboEmTela.Core.Speech;

namespace VerboEmTela.Windows.Audio;

public sealed class DefaultWindowsAudioOutput : IAudioOutput
{
    private readonly Func<IWavePlayer> _createPlayer;
    private readonly float _volume;
    private int _playing;

    public DefaultWindowsAudioOutput(float volume = 1f)
        : this(() => new WaveOutEvent { DeviceNumber = -1 }, volume)
    {
    }

    internal DefaultWindowsAudioOutput(Func<IWavePlayer> createPlayer, float volume = 1f)
    {
        if (!float.IsFinite(volume) || volume is < 0f or > 1f)
        {
            throw new ArgumentOutOfRangeException(nameof(volume), volume, "Volume must be between 0 and 1.");
        }

        _createPlayer = createPlayer;
        _volume = volume;
    }

    public async Task PlayAsync(SynthesizedSpeech speech, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(speech);
        if (speech.SampleRate <= 0 || speech.Samples.IsEmpty)
        {
            throw new ArgumentException("Playback requires samples and a positive sample rate.", nameof(speech));
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (Interlocked.CompareExchange(ref _playing, 1, 0) != 0)
        {
            throw new InvalidOperationException("This audio output is already playing.");
        }

        try
        {
            using var player = _createPlayer();
            var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            void OnStopped(object? sender, StoppedEventArgs args)
            {
                if (args.Exception is { } error)
                {
                    stopped.TrySetException(error);
                }
                else
                {
                    stopped.TrySetResult();
                }
            }

            player.PlaybackStopped += OnStopped;
            try
            {
                try
                {
                    player.Init(new MonoSamples(speech, _volume).ToWaveProvider16());
                }
                catch (MmException error) when (
                    error.Function == "waveOutOpen" &&
                    error.Result is MmResult.BadDeviceId or MmResult.AlreadyAllocated or MmResult.NoDriver)
                {
                    throw new AudioOutputUnavailableException(error);
                }

                cancellationToken.ThrowIfCancellationRequested();
                player.Play();
                try
                {
                    await stopped.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        player.Stop();
                    }
                    finally
                    {
                        await stopped.Task.ConfigureAwait(false);
                    }

                    throw;
                }
            }
            finally
            {
                player.PlaybackStopped -= OnStopped;
            }
        }
        finally
        {
            Volatile.Write(ref _playing, 0);
        }
    }

    private sealed class MonoSamples(SynthesizedSpeech speech, float volume) : ISampleProvider
    {
        private int _position;

        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(speech.SampleRate, 1);

        public int Read(float[] buffer, int offset, int count)
        {
            var available = Math.Min(count, speech.Samples.Length - _position);
            var output = buffer.AsSpan(offset, available);
            if (volume == 0f)
            {
                output.Clear();
            }
            else
            {
                var source = speech.Samples.Span.Slice(_position, available);
                for (var index = 0; index < available; index++)
                {
                    output[index] = source[index] * volume;
                }
            }

            _position += available;
            return available;
        }
    }
}
