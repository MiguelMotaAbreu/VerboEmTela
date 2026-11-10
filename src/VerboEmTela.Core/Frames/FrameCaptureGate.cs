namespace VerboEmTela.Core.Frames;

public sealed class FrameCaptureGate : IFrameCaptureGate
{
    private readonly object _synchronization = new();
    private TaskCompletionSource? _resumeSignal;

    public bool IsPaused
    {
        get
        {
            lock (_synchronization)
            {
                return _resumeSignal is not null;
            }
        }
    }

    public bool Pause()
    {
        lock (_synchronization)
        {
            if (_resumeSignal is not null)
            {
                return false;
            }

            _resumeSignal = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            return true;
        }
    }

    public bool Resume()
    {
        TaskCompletionSource? resumeSignal;

        lock (_synchronization)
        {
            resumeSignal = _resumeSignal;
            if (resumeSignal is null)
            {
                return false;
            }

            _resumeSignal = null;
        }

        return resumeSignal.TrySetResult();
    }

    public async ValueTask WaitUntilOpenAsync(
        CancellationToken cancellationToken = default)
    {
        while (true)
        {
            Task? waitTask;

            lock (_synchronization)
            {
                waitTask = _resumeSignal?.Task;
            }

            if (waitTask is null)
            {
                return;
            }

            await waitTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
