namespace VerboEmTela.Core.Detection;

public sealed record PanelObservation
{
    private PanelObservation(
        PanelMode mode,
        TimeSpan observedAt,
        string? sector = null,
        IdentityKind? identityKind = null,
        string? identity = null,
        string? destination = null)
    {
        if (observedAt < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(observedAt),
                "Observation time cannot be negative.");
        }

        Mode = mode;
        ObservedAt = observedAt;
        Sector = sector;
        IdentityKind = identityKind;
        Identity = identity;
        Destination = destination;
    }

    public PanelMode Mode { get; }

    public TimeSpan ObservedAt { get; }

    public string? Sector { get; }

    public IdentityKind? IdentityKind { get; }

    public string? Identity { get; }

    public string? Destination { get; }

    public static PanelObservation Invalid(TimeSpan observedAt) =>
        new(PanelMode.Invalid, observedAt);

    public static PanelObservation Dashboard(TimeSpan observedAt) =>
        new(PanelMode.Dashboard, observedAt);

    public static PanelObservation FullScreenCall(
        TimeSpan observedAt,
        string sector,
        string identity,
        string destination)
    {
        return FullScreenCall(
            observedAt,
            sector,
            global::VerboEmTela.Core.Detection.IdentityKind.Name,
            identity,
            destination);
    }

    public static PanelObservation FullScreenCall(
        TimeSpan observedAt,
        string sector,
        IdentityKind identityKind,
        string identity,
        string destination)
    {
        if (!Enum.IsDefined(identityKind))
        {
            throw new ArgumentOutOfRangeException(nameof(identityKind));
        }

        return new PanelObservation(
            PanelMode.FullScreenCall,
            observedAt,
            Required(sector, nameof(sector)),
            identityKind,
            Required(identity, nameof(identity)),
            Required(destination, nameof(destination)));
    }

    private static string Required(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value.Trim();
    }
}
