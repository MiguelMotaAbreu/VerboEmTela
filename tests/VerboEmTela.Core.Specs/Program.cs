using VerboEmTela.Core.Detection;

var specifications = new (string Name, Action Run)[]
{
    ("does not announce content already visible at startup", DoesNotAnnounceVisibleContentAtStartup),
    ("emits one event after a stable full-screen call", EmitsAfterStableFullScreenCall),
    ("does not duplicate an event while the call remains visible", DoesNotDuplicateWhileCallRemainsVisible),
    ("emits an identical recall after returning to the dashboard", EmitsIdenticalRecallAfterDashboard),
    ("requires a dashboard after the panel becomes invalid", RequiresDashboardAfterInvalidPanel),
    ("emits a distinct stable replacement without a dashboard", EmitsDistinctStableReplacement),
    ("rejects observations that arrive out of order", RejectsOutOfOrderObservations),
};

var failures = new List<string>();

foreach (var specification in specifications)
{
    try
    {
        specification.Run();
        Console.WriteLine($"PASS  {specification.Name}");
    }
    catch (Exception exception)
    {
        failures.Add(specification.Name);
        Console.Error.WriteLine($"FAIL  {specification.Name}");
        Console.Error.WriteLine($"      {exception.Message}");
    }
}

Console.WriteLine();
Console.WriteLine($"{specifications.Length - failures.Count}/{specifications.Length} specifications passed.");

return failures.Count == 0 ? 0 : 1;

static void DoesNotAnnounceVisibleContentAtStartup()
{
    var detector = new CallDetector();

    var events = ObserveStableCall(detector, 0, "CADASTRO", "PESSOA TESTE", "RECEPÇÃO 1");

    Equal(0, events.Count, "Startup content must be ignored until a dashboard is observed.");
    False(detector.IsArmed, "The detector must remain disarmed.");
}

static void EmitsAfterStableFullScreenCall()
{
    var detector = ArmedDetector();

    var events = ObserveStableCall(detector, 1_000, "CADASTRO", "PESSOA TESTE", "RECEPÇÃO 1");

    Equal(1, events.Count, "A stable call must produce one event.");
    Equal("CADASTRO", events[0].Sector, "Sector mismatch.");
    Equal("PESSOA TESTE", events[0].Identity, "Identity mismatch.");
    Equal("RECEPÇÃO 1", events[0].Destination, "Destination mismatch.");
}

static void DoesNotDuplicateWhileCallRemainsVisible()
{
    var detector = ArmedDetector();
    var events = ObserveStableCall(detector, 1_000, "CADASTRO", "PESSOA TESTE", "RECEPÇÃO 1");

    events.AddRange(ObserveStableCall(detector, 2_000, "CADASTRO", "PESSOA TESTE", "RECEPÇÃO 1"));

    Equal(1, events.Count, "A persistent call must not be emitted twice.");
}

static void EmitsIdenticalRecallAfterDashboard()
{
    var detector = ArmedDetector();
    var events = ObserveStableCall(detector, 1_000, "CADASTRO", "PESSOA TESTE", "RECEPÇÃO 1");

    detector.Process(PanelObservation.Dashboard(Ms(2_000)));
    events.AddRange(ObserveStableCall(detector, 3_000, "CADASTRO", "PESSOA TESTE", "RECEPÇÃO 1"));

    Equal(2, events.Count, "An identical call after a dashboard return is a legitimate recall.");
}

static void RequiresDashboardAfterInvalidPanel()
{
    var detector = ArmedDetector();
    detector.Process(PanelObservation.Invalid(Ms(1_000)));

    var ignored = ObserveStableCall(detector, 2_000, "ATENDIMENTO MÉDICO", "PESSOA TESTE", "SALA 6");
    Equal(0, ignored.Count, "Calls must be ignored while the detector is disarmed.");

    detector.Process(PanelObservation.Dashboard(Ms(3_000)));
    var accepted = ObserveStableCall(detector, 4_000, "ATENDIMENTO MÉDICO", "PESSOA TESTE", "SALA 6");
    Equal(1, accepted.Count, "A dashboard observation must rearm the detector.");
}

static void EmitsDistinctStableReplacement()
{
    var detector = ArmedDetector();
    var events = ObserveStableCall(detector, 1_000, "CADASTRO", "PESSOA A", "RECEPÇÃO 1");

    events.AddRange(ObserveStableCall(detector, 2_000, "ATENDIMENTO MÉDICO", "PESSOA B", "ODONTOLOGIA"));

    Equal(2, events.Count, "A different stable call may replace another full-screen call.");
    Equal("PESSOA B", events[1].Identity, "The replacement event was not emitted.");
}

static void RejectsOutOfOrderObservations()
{
    var detector = new CallDetector();
    detector.Process(PanelObservation.Dashboard(Ms(1_000)));

    Throws<ArgumentException>(
        () => detector.Process(PanelObservation.Dashboard(Ms(999))),
        "Out-of-order observations must fail fast.");
}

static CallDetector ArmedDetector()
{
    var detector = new CallDetector();
    detector.Process(PanelObservation.Dashboard(TimeSpan.Zero));
    return detector;
}

static List<DetectedCall> ObserveStableCall(
    CallDetector detector,
    int startMilliseconds,
    string sector,
    string identity,
    string destination)
{
    var events = new List<DetectedCall>();

    foreach (var offset in new[] { 0, 300, 600 })
    {
        var detected = detector.Process(
            PanelObservation.FullScreenCall(
                Ms(startMilliseconds + offset),
                sector,
                identity,
                destination));

        if (detected is not null)
        {
            events.Add(detected);
        }
    }

    return events;
}

static TimeSpan Ms(int milliseconds) => TimeSpan.FromMilliseconds(milliseconds);

static void Equal<T>(T expected, T actual, string message)
    where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message} Expected: {expected}. Actual: {actual}.");
    }
}

static void False(bool value, string message)
{
    if (value)
    {
        throw new InvalidOperationException(message);
    }
}

static void Throws<TException>(Action action, string message)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException(message);
}
