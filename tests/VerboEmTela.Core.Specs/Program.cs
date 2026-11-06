using System.Diagnostics.CodeAnalysis;
using VerboEmTela.Core.Detection;
using VerboEmTela.Core.Imaging;
using VerboEmTela.Core.Layout;
using VerboEmTela.Core.Processing;

var specifications = new (string Name, Action Run)[]
{
    ("does not announce content already visible at startup", DoesNotAnnounceVisibleContentAtStartup),
    ("emits one event after a stable full-screen call", EmitsAfterStableFullScreenCall),
    ("does not duplicate an event while the call remains visible", DoesNotDuplicateWhileCallRemainsVisible),
    ("emits an identical recall after returning to the dashboard", EmitsIdenticalRecallAfterDashboard),
    ("requires a dashboard after the panel becomes invalid", RequiresDashboardAfterInvalidPanel),
    ("emits a distinct stable replacement without a dashboard", EmitsDistinctStableReplacement),
    ("rejects observations that arrive out of order", RejectsOutOfOrderObservations),
    ("recognizes the three-section dashboard", RecognizesDashboard),
    ("recognizes a full-screen call", RecognizesFullScreenCall),
    ("rejects an unexpected resolution", RejectsUnexpectedResolution),
    ("rejects a narrow green banner from another screen", RejectsNarrowGreenBanner),
    ("rejects an incomplete two-band layout", RejectsIncompleteLayout),
    ("rejects a substantially occluded panel", RejectsOccludedPanel),
    ("tolerates a moderate variation of the panel green", ToleratesGreenVariation),
    ("does not extract text from a call already visible at startup", DoesNotExtractStartupCall),
    ("selects the name before the password from the same frame", SelectsNameBeforePassword),
    ("uses a password when a name is absent", UsesPasswordWhenNameIsAbsent),
    ("waits for complete text without disarming the detector", WaitsForCompleteText),
    ("disarms the frame pipeline when the layout becomes invalid", PipelineDisarmsOnInvalidLayout),
    ("detects an identical recall through the frame pipeline", PipelineDetectsIdenticalRecall),
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

static void RecognizesDashboard()
{
    var recognition = TestLayoutRecognizer().Recognize(
        SyntheticFrame((25, 5, 0.90), (52, 5, 0.90), (79, 5, 0.90)));

    Equal(PanelMode.Dashboard, recognition.Mode, "The three-section layout was not recognized.");
    Equal(3, recognition.GreenBands.Count, "The dashboard must expose three structural bands.");
    Equal(LayoutRejectionReason.None, recognition.RejectionReason, "A valid panel cannot be rejected.");
}

static void RecognizesFullScreenCall()
{
    var recognition = TestLayoutRecognizer().Recognize(
        SyntheticFrame((22, 6, 0.90)));

    Equal(PanelMode.FullScreenCall, recognition.Mode, "The full-screen call was not recognized.");
    Equal(1, recognition.GreenBands.Count, "A full-screen call must expose one structural band.");
}

static void RejectsUnexpectedResolution()
{
    var frame = SolidFrame(191, 108, 245, 245, 245);
    var recognition = TestLayoutRecognizer().Recognize(frame);

    Equal(PanelMode.Invalid, recognition.Mode, "An unexpected resolution must be rejected.");
    Equal(
        LayoutRejectionReason.UnexpectedDimensions,
        recognition.RejectionReason,
        "Resolution rejection reason mismatch.");
}

static void RejectsNarrowGreenBanner()
{
    var recognition = TestLayoutRecognizer().Recognize(
        SyntheticFrame((22, 6, 0.50)));

    Equal(PanelMode.Invalid, recognition.Mode, "A narrow banner cannot identify the SoulMV panel.");
    Equal(
        LayoutRejectionReason.UnexpectedGreenBandCount,
        recognition.RejectionReason,
        "Narrow-banner rejection reason mismatch.");
}

static void RejectsIncompleteLayout()
{
    var recognition = TestLayoutRecognizer().Recognize(
        SyntheticFrame((25, 5, 0.90), (52, 5, 0.90)));

    Equal(PanelMode.Invalid, recognition.Mode, "A two-section fragment must not be accepted.");
}

static void RejectsOccludedPanel()
{
    var recognition = TestLayoutRecognizer().Recognize(
        SyntheticFrame((25, 5, 0.60), (52, 5, 0.60), (79, 5, 0.60)));

    Equal(PanelMode.Invalid, recognition.Mode, "A substantially occluded panel must be rejected.");
}

static void ToleratesGreenVariation()
{
    var recognition = TestLayoutRecognizer().Recognize(
        SyntheticFrameWithGreen((61, 112, 46), (22, 6, 0.90)));

    Equal(PanelMode.FullScreenCall, recognition.Mode, "A moderate color variation must remain recognizable.");
}

static void DoesNotExtractStartupCall()
{
    var extractor = CompleteExtractor();
    var processor = TestFrameProcessor(extractor);

    var result = processor.Process(SyntheticFrame((22, 6, 0.90)), Ms(1_000));

    Equal(
        FrameProcessingStatus.IgnoredUntilDashboard,
        result.Status,
        "Startup content must be ignored until a dashboard is seen.");
    Equal(0, extractor.InvocationCount, "Ignored startup content must not be sent to OCR.");
}

static void SelectsNameBeforePassword()
{
    var extractor = new StubTextExtractor(new ExtractedCallText(
        "CADASTRO",
        "PESSOA TESTE",
        "PUC0073",
        "RECEPÇÃO 1"));
    var processor = TestFrameProcessor(extractor);

    processor.Process(DashboardFrame(), TimeSpan.Zero);
    var detected = ProcessStableFrameCall(processor, 1_000);

    NotNull(detected, "A complete stable call must be detected.");
    Equal(IdentityKind.Name, detected.IdentityKind, "Name must have priority over password.");
    Equal("PESSOA TESTE", detected.Identity, "Selected identity mismatch.");
}

static void UsesPasswordWhenNameIsAbsent()
{
    var extractor = new StubTextExtractor(new ExtractedCallText(
        "CADASTRO",
        null,
        "PUC0073",
        "RECEPÇÃO 1"));
    var processor = TestFrameProcessor(extractor);

    processor.Process(DashboardFrame(), TimeSpan.Zero);
    var detected = ProcessStableFrameCall(processor, 1_000);

    NotNull(detected, "A password-only call must be detected.");
    Equal(IdentityKind.Password, detected.IdentityKind, "Password identity kind mismatch.");
    Equal("PUC0073", detected.Identity, "Password value mismatch.");
}

static void WaitsForCompleteText()
{
    var extractor = new StubTextExtractor(new ExtractedCallText(
        "CADASTRO",
        "PESSOA TESTE",
        null,
        null));
    var processor = TestFrameProcessor(extractor);
    var callFrame = SyntheticFrame((22, 6, 0.90));

    processor.Process(DashboardFrame(), TimeSpan.Zero);
    var first = processor.Process(callFrame, Ms(1_000));
    var second = processor.Process(callFrame, Ms(1_300));

    Equal(FrameProcessingStatus.IncompleteCall, first.Status, "Missing destination must reject the frame.");
    Equal(FrameProcessingStatus.IncompleteCall, second.Status, "Incomplete frames must remain rejected.");

    extractor.Result = new ExtractedCallText(
        "CADASTRO",
        "PESSOA TESTE",
        null,
        "RECEPÇÃO 1");

    var detected = ProcessStableFrameCall(processor, 1_600);
    NotNull(detected, "Complete frames after a partial reading must still be accepted.");
}

static void PipelineDisarmsOnInvalidLayout()
{
    var extractor = CompleteExtractor();
    var processor = TestFrameProcessor(extractor);

    processor.Process(DashboardFrame(), TimeSpan.Zero);
    var invalid = processor.Process(SolidFrame(192, 108, 40, 40, 40), Ms(1_000));
    var ignored = processor.Process(SyntheticFrame((22, 6, 0.90)), Ms(2_000));

    Equal(FrameProcessingStatus.InvalidLayout, invalid.Status, "Invalid layout status mismatch.");
    Equal(
        FrameProcessingStatus.IgnoredUntilDashboard,
        ignored.Status,
        "A call after an invalid layout must wait for a dashboard.");
    Equal(0, extractor.InvocationCount, "Disarmed calls must not be extracted.");
}

static void PipelineDetectsIdenticalRecall()
{
    var extractor = CompleteExtractor();
    var processor = TestFrameProcessor(extractor);

    processor.Process(DashboardFrame(), TimeSpan.Zero);
    var first = ProcessStableFrameCall(processor, 1_000);
    processor.Process(DashboardFrame(), Ms(2_000));
    var recall = ProcessStableFrameCall(processor, 3_000);

    NotNull(first, "The first occurrence must be detected.");
    NotNull(recall, "The identical recall must be detected after the dashboard return.");
    Equal(first.Identity, recall.Identity, "Recall identity mismatch.");
    Equal(first.Destination, recall.Destination, "Recall destination mismatch.");
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

static PanelLayoutRecognizer TestLayoutRecognizer()
{
    return new PanelLayoutRecognizer(new PanelLayoutRecognizerOptions
    {
        RequiredWidth = 192,
        RequiredHeight = 108,
        MinimumBandHeightRatio = 0.02,
    });
}

static CallFrameProcessor TestFrameProcessor(StubTextExtractor extractor)
{
    return new CallFrameProcessor(TestLayoutRecognizer(), extractor);
}

static StubTextExtractor CompleteExtractor()
{
    return new StubTextExtractor(new ExtractedCallText(
        "CADASTRO",
        "PESSOA TESTE",
        null,
        "RECEPÇÃO 1"));
}

static Rgb24Frame DashboardFrame()
{
    return SyntheticFrame((25, 5, 0.90), (52, 5, 0.90), (79, 5, 0.90));
}

static DetectedCall? ProcessStableFrameCall(CallFrameProcessor processor, int startMilliseconds)
{
    DetectedCall? detected = null;
    var callFrame = SyntheticFrame((22, 6, 0.90));

    foreach (var offset in new[] { 0, 300, 600 })
    {
        var result = processor.Process(callFrame, Ms(startMilliseconds + offset));
        detected ??= result.DetectedCall;
    }

    return detected;
}

static Rgb24Frame SyntheticFrame(params (int Top, int Height, double WidthRatio)[] bands)
{
    return SyntheticFrameWithGreen((50, 90, 30), bands);
}

static Rgb24Frame SyntheticFrameWithGreen(
    (byte Red, byte Green, byte Blue) green,
    params (int Top, int Height, double WidthRatio)[] bands)
{
    const int width = 192;
    const int height = 108;
    var pixels = FilledPixels(width, height, 245, 245, 245);

    foreach (var band in bands)
    {
        var bandWidth = (int)Math.Floor(width * band.WidthRatio);
        var left = (width - bandWidth) / 2;

        FillRectangle(
            pixels,
            width,
            left,
            band.Top,
            bandWidth,
            band.Height,
            green.Red,
            green.Green,
            green.Blue);
    }

    return new Rgb24Frame(width, height, pixels);
}

static Rgb24Frame SolidFrame(
    int width,
    int height,
    byte red,
    byte green,
    byte blue)
{
    return new Rgb24Frame(width, height, FilledPixels(width, height, red, green, blue));
}

static byte[] FilledPixels(
    int width,
    int height,
    byte red,
    byte green,
    byte blue)
{
    var pixels = new byte[width * height * 3];

    for (var index = 0; index < pixels.Length; index += 3)
    {
        pixels[index] = red;
        pixels[index + 1] = green;
        pixels[index + 2] = blue;
    }

    return pixels;
}

static void FillRectangle(
    byte[] pixels,
    int frameWidth,
    int left,
    int top,
    int width,
    int height,
    byte red,
    byte green,
    byte blue)
{
    for (var y = top; y < top + height; y++)
    {
        for (var x = left; x < left + width; x++)
        {
            var index = ((y * frameWidth) + x) * 3;
            pixels[index] = red;
            pixels[index + 1] = green;
            pixels[index + 2] = blue;
        }
    }
}

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

static void NotNull([NotNull] object? value, string message)
{
    if (value is null)
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

sealed class StubTextExtractor : IFrameTextExtractor
{
    public StubTextExtractor(ExtractedCallText result)
    {
        Result = result;
    }

    public ExtractedCallText Result { get; set; }

    public int InvocationCount { get; private set; }

    public ExtractedCallText ExtractCall(Rgb24Frame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        InvocationCount++;
        return Result;
    }
}
