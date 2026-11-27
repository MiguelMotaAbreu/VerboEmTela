using VerboEmTela.Core.Speech;

namespace VerboEmTela.Core.Announcements;

public enum AnnouncementPlaybackResult
{
    NoPendingAnnouncement,
    Busy,
    Expired,
    Played,
}

public sealed class AnnouncementQueuePlayer
{
    private readonly AnnouncementQueue _queue;
    private readonly AnnouncementPlayback _playback;
    private readonly TimeSpan _startDelay;
    private int _playing;

    public AnnouncementQueuePlayer(
        AnnouncementQueue queue,
        ISpeechEngine speech,
        IAudioOutput output,
        TimeSpan startDelay = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(startDelay, TimeSpan.Zero);
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _playback = new AnnouncementPlayback(speech, output, queue.Clock);
        _startDelay = startDelay;
    }

    public async Task<AnnouncementPlaybackResult> PlayNextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Interlocked.CompareExchange(ref _playing, 1, 0) != 0)
        {
            return AnnouncementPlaybackResult.Busy;
        }

        try
        {
            var announcement = _queue.Dequeue();
            if (announcement is null)
            {
                return AnnouncementPlaybackResult.NoPendingAnnouncement;
            }

            var played = await _playback.PlayIfFreshAsync(
                announcement,
                _startDelay,
                cancellationToken).ConfigureAwait(false);
            return played ? AnnouncementPlaybackResult.Played : AnnouncementPlaybackResult.Expired;
        }
        finally
        {
            Volatile.Write(ref _playing, 0);
        }
    }
}
