namespace VerboEmTela.Windows.Application;

public sealed class SingleInstanceLease : IDisposable
{
    private Mutex? _mutex;

    private SingleInstanceLease(string instanceName, Mutex mutex)
    {
        InstanceName = instanceName;
        _mutex = mutex;
    }

    public string InstanceName { get; }

    public static SingleInstanceLease? TryAcquire(string instanceName)
    {
        if (string.IsNullOrWhiteSpace(instanceName))
        {
            throw new ArgumentException(
                "An application instance name is required.",
                nameof(instanceName));
        }

        Mutex? mutex = null;

        try
        {
            mutex = new Mutex(
                initiallyOwned: false,
                instanceName.Trim(),
                out var createdNew);

            if (!createdNew)
            {
                mutex.Dispose();
                return null;
            }

            return new SingleInstanceLease(instanceName.Trim(), mutex);
        }
        catch
        {
            mutex?.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref _mutex, null)?.Dispose();
    }
}
