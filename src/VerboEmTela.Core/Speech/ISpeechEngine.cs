namespace VerboEmTela.Core.Speech;

public interface ISpeechEngine
{
    SynthesizedSpeech Synthesize(string text, float speed = 1f);
}
