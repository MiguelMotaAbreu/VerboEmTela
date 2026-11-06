namespace VerboEmTela.Core.Application;

public sealed record ApplicationStatus
{
    internal ApplicationStatus(ApplicationState state, string? errorReason = null)
    {
        State = state;
        ErrorReason = errorReason;
    }

    public ApplicationState State { get; }

    public string? ErrorReason { get; }
}
