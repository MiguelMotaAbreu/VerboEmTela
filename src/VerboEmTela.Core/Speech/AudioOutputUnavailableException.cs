namespace VerboEmTela.Core.Speech;

// The output device could not be opened, so playback has not begun.
public sealed class AudioOutputUnavailableException(Exception innerException)
    : IOException("The audio output device is temporarily unavailable.", innerException)
{
}
