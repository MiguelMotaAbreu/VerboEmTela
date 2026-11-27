namespace VerboEmTela.Core.Speech;

public interface IAudioOutput
{
    Task PlayAsync(SynthesizedSpeech speech, CancellationToken cancellationToken = default);
}
