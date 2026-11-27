using VerboEmTela.Core.Detection;

namespace VerboEmTela.Core.Announcements;

public enum AnnouncementEnqueueResult
{
    Accepted,
    Expired,
    Full,
}

public sealed class AnnouncementQueue
{
    private readonly object _sync = new();
    private readonly LinkedList<Announcement> _pending = new();
    private readonly int _capacity;
    private readonly TimeProvider _clock;

    public AnnouncementQueue(int capacity, TimeProvider? clock = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _capacity = capacity;
        _clock = clock ?? TimeProvider.System;
    }

    internal TimeProvider Clock => _clock;

    public AnnouncementEnqueueResult Enqueue(DetectedCall call, DestinationPronunciationMap? pronunciations = null)
    {
        ArgumentNullException.ThrowIfNull(call);
        if (call.DetectionTimestamp is not { } detection ||
            call.VisualStartTimestamp is not { } visualStart ||
            !ReferenceEquals(detection.Clock, _clock) ||
            !ReferenceEquals(visualStart.Clock, _clock))
        {
            throw new ArgumentException("The call timestamps must use the queue's clock.", nameof(call));
        }

        return Enqueue(CallUtteranceFormatter.Format(call, pronunciations), detection.Value, visualStart.Value);
    }

    public AnnouncementEnqueueResult Enqueue(string text, long detectedTimestamp) =>
        Enqueue(text, detectedTimestamp, detectedTimestamp);

    // Both timestamps must come from GetTimestamp() on the same clock.
    public AnnouncementEnqueueResult Enqueue(string text, long detectedTimestamp, long visualStartTimestamp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (_clock.GetElapsedTime(visualStartTimestamp, detectedTimestamp) < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(visualStartTimestamp), "Visual start cannot follow detection.");
        }

        lock (_sync)
        {
            var now = _clock.GetTimestamp();
            var expired = AnnouncementLifetime.HasExpired(_clock, detectedTimestamp, now);
            RemoveExpired(now);
            if (expired)
            {
                return AnnouncementEnqueueResult.Expired;
            }

            if (_pending.Count == _capacity)
            {
                return AnnouncementEnqueueResult.Full;
            }

            _pending.AddLast(new Announcement(text, detectedTimestamp, visualStartTimestamp));
            return AnnouncementEnqueueResult.Accepted;
        }
    }

    public Announcement? Dequeue()
    {
        lock (_sync)
        {
            RemoveExpired(_clock.GetTimestamp());
            if (_pending.First is not { } oldest)
            {
                return null;
            }

            _pending.RemoveFirst();
            return oldest.Value;
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            _pending.Clear();
        }
    }

    private void RemoveExpired(long now)
    {
        for (var node = _pending.First; node is not null;)
        {
            var next = node.Next;
            if (AnnouncementLifetime.HasExpired(_clock, node.Value.DetectedTimestamp, now))
            {
                _pending.Remove(node);
            }

            node = next;
        }
    }
}
