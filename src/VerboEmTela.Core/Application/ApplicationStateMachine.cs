namespace VerboEmTela.Core.Application;

public sealed class ApplicationStateMachine
{
    private readonly object _synchronization = new();
    private readonly ITransientContentCleaner _contentCleaner;
    private ApplicationStatus _current = new(ApplicationState.Starting);

    public ApplicationStateMachine(ITransientContentCleaner contentCleaner)
    {
        _contentCleaner = contentCleaner ??
            throw new ArgumentNullException(nameof(contentCleaner));
    }

    public ApplicationStatus Current
    {
        get
        {
            lock (_synchronization)
            {
                return _current;
            }
        }
    }

    public ApplicationStatus CompleteStartup()
    {
        return Transition(
            ApplicationState.Arming,
            clearTransientContent: false,
            ApplicationState.Starting);
    }

    public ApplicationStatus ConfirmPanelReady()
    {
        return Transition(
            ApplicationState.Monitoring,
            clearTransientContent: false,
            ApplicationState.Arming);
    }

    public ApplicationStatus ReportPanelLost()
    {
        return Transition(
            ApplicationState.Arming,
            clearTransientContent: true,
            ApplicationState.Monitoring,
            ApplicationState.Speaking);
    }

    public ApplicationStatus BeginSpeaking()
    {
        return Transition(
            ApplicationState.Speaking,
            clearTransientContent: false,
            ApplicationState.Monitoring);
    }

    public ApplicationStatus CompleteSpeaking()
    {
        return Transition(
            ApplicationState.Monitoring,
            clearTransientContent: false,
            ApplicationState.Speaking);
    }

    public ApplicationStatus Pause()
    {
        return Transition(
            ApplicationState.Paused,
            clearTransientContent: true,
            ApplicationState.Arming,
            ApplicationState.Monitoring,
            ApplicationState.Speaking);
    }

    public ApplicationStatus Resume()
    {
        return Transition(
            ApplicationState.Arming,
            clearTransientContent: true,
            ApplicationState.Paused);
    }

    public ApplicationStatus Fail(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "An application error reason is required.",
                nameof(reason));
        }

        lock (_synchronization)
        {
            if (_current.State is ApplicationState.Stopped)
            {
                throw InvalidTransition(ApplicationState.Error);
            }

            _current = new ApplicationStatus(
                ApplicationState.Error,
                reason.Trim());
            _contentCleaner.Clear();
            return _current;
        }
    }

    public ApplicationStatus Recover()
    {
        return Transition(
            ApplicationState.Arming,
            clearTransientContent: true,
            ApplicationState.Error);
    }

    public ApplicationStatus Stop()
    {
        lock (_synchronization)
        {
            if (_current.State is ApplicationState.Stopped)
            {
                throw InvalidTransition(ApplicationState.Stopped);
            }

            _current = new ApplicationStatus(ApplicationState.Stopped);
            _contentCleaner.Clear();
            return _current;
        }
    }

    private ApplicationStatus Transition(
        ApplicationState target,
        bool clearTransientContent,
        params ApplicationState[] allowedSources)
    {
        lock (_synchronization)
        {
            if (!allowedSources.Contains(_current.State))
            {
                throw InvalidTransition(target);
            }

            _current = new ApplicationStatus(target);

            if (clearTransientContent)
            {
                _contentCleaner.Clear();
            }

            return _current;
        }
    }

    private InvalidOperationException InvalidTransition(ApplicationState target)
    {
        return new InvalidOperationException(
            $"Application state cannot change from {_current.State} to {target}.");
    }
}
