namespace VerboEmTela.Core.Speech;

public sealed record SynthesizedSpeech(ReadOnlyMemory<float> Samples, int SampleRate);
