using VerboEmTela.Core.Frames;

namespace VerboEmTela.Core.Detection;

public sealed class CallDetector
{
    private readonly CallDetectorOptions _options;
    private Candidate? _candidate;
    private ObservationKey? _lastEmittedKey;
    private ObservationTime? _firstIncompleteFullScreen;
    private VisualPresentation? _presentation;
    private TimeSpan? _lastObservationTime;
    private bool _armed;

    public CallDetector(CallDetectorOptions? options = null)
    {
        _options = options ?? new CallDetectorOptions();
        _options.Validate();
    }

    public bool IsArmed => _armed;

    public void DiscardPendingCandidate()
    {
        _candidate = null;
    }

    internal void ObserveIncompleteFullScreen(TimeSpan observedAt, MonotonicTimestamp? timestamp)
    {
        EnsureMonotonicTime(observedAt);
        if (_presentation is null)
        {
            _firstIncompleteFullScreen ??= new ObservationTime(observedAt, timestamp);
        }

        DiscardPendingCandidate();
    }

    public DetectedCall? Process(PanelObservation observation) => Process(observation, null);

    internal DetectedCall? Process(PanelObservation observation, MonotonicTimestamp? timestamp)
    {
        ArgumentNullException.ThrowIfNull(observation);
        EnsureMonotonicTime(observation.ObservedAt);

        return observation.Mode switch
        {
            PanelMode.Invalid => HandleInvalid(),
            PanelMode.Dashboard => HandleDashboard(),
            PanelMode.FullScreenCall => HandleFullScreenCall(observation, timestamp),
            _ => throw new ArgumentOutOfRangeException(
                nameof(observation),
                observation.Mode,
                "Unsupported panel mode."),
        };
    }

    public void Reset()
    {
        ClearPresentation();
        _lastObservationTime = null;
        _armed = false;
    }

    private DetectedCall? HandleInvalid()
    {
        ClearPresentation();
        _armed = false;
        return null;
    }

    private DetectedCall? HandleDashboard()
    {
        ClearPresentation();
        _armed = true;
        return null;
    }

    private DetectedCall? HandleFullScreenCall(PanelObservation observation, MonotonicTimestamp? timestamp)
    {
        if (!_armed)
        {
            return null;
        }

        var key = ObservationKey.From(observation);
        if (_presentation is null)
        {
            var start = _firstIncompleteFullScreen ?? new ObservationTime(observation.ObservedAt, timestamp);
            _presentation = new VisualPresentation(key, start);
        }
        else if (_presentation.Key != key)
        {
            _presentation = new VisualPresentation(key, new ObservationTime(observation.ObservedAt, timestamp));
        }

        if (_candidate is null || _candidate.Key != key)
        {
            _candidate = new Candidate(key, observation.ObservedAt);
            return null;
        }

        _candidate = _candidate with
        {
            LastSeenAt = observation.ObservedAt,
            ObservationCount = _candidate.ObservationCount + 1,
        };

        if (_candidate.ObservationCount < _options.MinimumStableObservations ||
            _candidate.LastSeenAt - _candidate.FirstSeenAt < _options.MinimumStableDuration ||
            _lastEmittedKey == key)
        {
            return null;
        }

        _lastEmittedKey = key;

        return new DetectedCall(
            key.Sector,
            key.IdentityKind,
            key.Identity,
            key.Destination,
            observation.ObservedAt)
        {
            VisualStartedAt = _presentation.StartedAt.Elapsed,
            VisualStartTimestamp = _presentation.StartedAt.Timestamp,
            DetectionTimestamp = timestamp,
        };
    }

    private void ClearPresentation()
    {
        _candidate = null;
        _lastEmittedKey = null;
        _firstIncompleteFullScreen = null;
        _presentation = null;
    }

    private void EnsureMonotonicTime(TimeSpan observedAt)
    {
        if (_lastObservationTime is not null && observedAt < _lastObservationTime)
        {
            throw new ArgumentException(
                "Observations must be processed in chronological order.",
                nameof(observedAt));
        }

        _lastObservationTime = observedAt;
    }

    private sealed record ObservationKey(
        string Sector,
        IdentityKind IdentityKind,
        string Identity,
        string Destination)
    {
        public static ObservationKey From(PanelObservation observation)
        {
            return new ObservationKey(
                observation.Sector!,
                observation.IdentityKind!.Value,
                observation.Identity!,
                observation.Destination!);
        }
    }

    private readonly record struct ObservationTime(TimeSpan Elapsed, MonotonicTimestamp? Timestamp);

    private sealed record VisualPresentation(ObservationKey Key, ObservationTime StartedAt);

    private sealed record Candidate(
        ObservationKey Key,
        TimeSpan FirstSeenAt,
        TimeSpan LastSeenAt,
        int ObservationCount)
    {
        public Candidate(ObservationKey key, TimeSpan firstSeenAt)
            : this(key, firstSeenAt, firstSeenAt, 1)
        {
        }
    }
}
