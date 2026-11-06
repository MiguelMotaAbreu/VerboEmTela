namespace VerboEmTela.Core.Detection;

public sealed class CallDetector
{
    private readonly CallDetectorOptions _options;
    private Candidate? _candidate;
    private ObservationKey? _lastEmittedKey;
    private TimeSpan? _lastObservationTime;
    private bool _armed;

    public CallDetector(CallDetectorOptions? options = null)
    {
        _options = options ?? new CallDetectorOptions();
        _options.Validate();
    }

    public bool IsArmed => _armed;

    public DetectedCall? Process(PanelObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        EnsureMonotonicTime(observation.ObservedAt);

        return observation.Mode switch
        {
            PanelMode.Invalid => HandleInvalid(),
            PanelMode.Dashboard => HandleDashboard(),
            PanelMode.FullScreenCall => HandleFullScreenCall(observation),
            _ => throw new ArgumentOutOfRangeException(
                nameof(observation),
                observation.Mode,
                "Unsupported panel mode."),
        };
    }

    public void Reset()
    {
        _candidate = null;
        _lastEmittedKey = null;
        _lastObservationTime = null;
        _armed = false;
    }

    private DetectedCall? HandleInvalid()
    {
        _candidate = null;
        _lastEmittedKey = null;
        _armed = false;
        return null;
    }

    private DetectedCall? HandleDashboard()
    {
        _candidate = null;
        _lastEmittedKey = null;
        _armed = true;
        return null;
    }

    private DetectedCall? HandleFullScreenCall(PanelObservation observation)
    {
        if (!_armed)
        {
            return null;
        }

        var key = ObservationKey.From(observation);

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
            key.Identity,
            key.Destination,
            observation.ObservedAt);
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
        string Identity,
        string Destination)
    {
        public static ObservationKey From(PanelObservation observation)
        {
            return new ObservationKey(
                observation.Sector!,
                observation.Identity!,
                observation.Destination!);
        }
    }

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
