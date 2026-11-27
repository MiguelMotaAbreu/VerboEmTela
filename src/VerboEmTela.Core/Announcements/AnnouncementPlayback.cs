using VerboEmTela.Core.Speech;

namespace VerboEmTela.Core.Announcements;

public sealed class AnnouncementPlayback
{
    private static readonly TimeSpan RetryInterval = TimeSpan.FromMilliseconds(500);

    private readonly ISpeechEngine _speech;
    private readonly IAudioOutput _output;
    private readonly TimeProvider _clock;

    public AnnouncementPlayback(ISpeechEngine speech, IAudioOutput output, TimeProvider? clock = null)
    {
        _speech = speech ?? throw new ArgumentNullException(nameof(speech));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _clock = clock ?? TimeProvider.System;
    }

    // detectedTimestamp must come from GetTimestamp() on the same clock.
    public Task<bool> PlayIfFreshAsync(
        string text,
        long detectedTimestamp,
        CancellationToken cancellationToken = default) =>
        PlayIfFreshAsync(new Announcement(text, detectedTimestamp, detectedTimestamp), TimeSpan.Zero, cancellationToken);

    internal async Task<bool> PlayIfFreshAsync(
        Announcement announcement,
        TimeSpan startDelay,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(announcement.Text);
        cancellationToken.ThrowIfCancellationRequested();
        if (GetWaitBeforePlayback(announcement, startDelay) is null)
        {
            return false;
        }

        var audio = _speech.Synthesize(announcement.Text);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (GetWaitBeforePlayback(announcement, startDelay) is not { } wait)
            {
                return false;
            }

            if (wait > TimeSpan.Zero)
            {
                await Task.Delay(wait, _clock, cancellationToken).ConfigureAwait(false);
                continue;
            }

            try
            {
                await _output.PlayAsync(audio, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (AudioOutputUnavailableException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var remaining = AnnouncementLifetime.Remaining(_clock, announcement.DetectedTimestamp, _clock.GetTimestamp());
                if (remaining <= TimeSpan.Zero)
                {
                    return false;
                }

                var delay = remaining < RetryInterval ? remaining : RetryInterval;
                await Task.Delay(delay, _clock, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private TimeSpan? GetWaitBeforePlayback(Announcement announcement, TimeSpan startDelay)
    {
        var now = _clock.GetTimestamp();
        var remaining = AnnouncementLifetime.Remaining(_clock, announcement.DetectedTimestamp, now);
        var wait = startDelay - _clock.GetElapsedTime(announcement.VisualStartTimestamp, now);
        return remaining > TimeSpan.Zero && wait < remaining ? wait : null;
    }
}
