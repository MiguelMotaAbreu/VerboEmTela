using System.Diagnostics.CodeAnalysis;
using VerboEmTela.Core.Detection;
using VerboEmTela.Core.Frames;
using VerboEmTela.Core.Imaging;
using VerboEmTela.Core.Layout;
using VerboEmTela.Core.Processing;
using VerboEmTela.Windows.Capture;

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
    ("processes a timed frame sequence in order", () => RunnerProcessesTimedSequence().GetAwaiter().GetResult()),
    ("resets processing state after a sequence ends", () => RunnerResetsAfterSequence().GetAwaiter().GetResult()),
    ("rejects an out-of-order frame sequence", RejectsOutOfOrderFrameSequence),
    ("resets processing state when a sequence is cancelled", () => CancelledRunnerResetsState().GetAwaiter().GetResult()),
    ("converts padded BGR pixels into packed RGB", ConvertsPaddedBgrToPackedRgb),
    ("streams screen frames from an explicit region", () => StreamsScreenFrames().GetAwaiter().GetResult()),
    ("selects the only monitor containing the panel", SelectsUniquePanelMonitor),
    ("reports when no monitor contains the panel", ReportsNoPanelMonitor),
    ("rejects an ambiguous panel monitor selection", RejectsAmbiguousPanelMonitors),
    ("reports when Windows exposes no monitors", ReportsNoAvailableMonitors),
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

static async Task RunnerProcessesTimedSequence()
{
    var extractor = CompleteExtractor();
    var processor = TestFrameProcessor(extractor);
    var runner = new FrameProcessingRunner(processor);
    var source = StableCallSequence();

    var results = await CollectAsync(runner.RunAsync(source));
    var calls = results
        .Where(result => result.DetectedCall is not null)
        .Select(result => result.DetectedCall!)
        .ToArray();

    Equal(4, results.Count, "Every source frame must produce one processing result.");
    Equal(1, calls.Length, "The timed sequence must produce one detected call.");
    Equal("PESSOA TESTE", calls[0].Identity, "Detected identity mismatch.");
}

static async Task RunnerResetsAfterSequence()
{
    var extractor = CompleteExtractor();
    var processor = TestFrameProcessor(extractor);
    var runner = new FrameProcessingRunner(processor);

    _ = await CollectAsync(runner.RunAsync(StableCallSequence()));

    var resultAfterCompletion = processor.Process(
        SyntheticFrame((22, 6, 0.90)),
        Ms(3_000));

    Equal(
        FrameProcessingStatus.IgnoredUntilDashboard,
        resultAfterCompletion.Status,
        "A completed run must not leave the processor armed.");
}

static void RejectsOutOfOrderFrameSequence()
{
    var callFrame = SyntheticFrame((22, 6, 0.90));

    Throws<ArgumentException>(
        () => _ = new FrameSequenceSource(new[]
        {
            new CapturedFrame(callFrame, Ms(1_000)),
            new CapturedFrame(callFrame, Ms(999)),
        }),
        "An out-of-order frame sequence must be rejected before processing.");
}

static async Task CancelledRunnerResetsState()
{
    var extractor = CompleteExtractor();
    var processor = TestFrameProcessor(extractor);
    var runner = new FrameProcessingRunner(processor);
    using var cancellation = new CancellationTokenSource();
    var cancellationObserved = false;

    try
    {
        await foreach (var result in runner.RunAsync(StableCallSequence(), cancellation.Token))
        {
            Equal(
                FrameProcessingStatus.DashboardObserved,
                result.Status,
                "The first replay frame must be the dashboard.");
            cancellation.Cancel();
        }
    }
    catch (OperationCanceledException)
    {
        cancellationObserved = true;
    }

    True(cancellationObserved, "Cancellation must stop sequence processing.");

    var resultAfterCancellation = processor.Process(
        SyntheticFrame((22, 6, 0.90)),
        Ms(3_000));

    Equal(
        FrameProcessingStatus.IgnoredUntilDashboard,
        resultAfterCancellation.Status,
        "Cancellation must clear the armed state.");
}

static void ConvertsPaddedBgrToPackedRgb()
{
    var source = new byte[]
    {
        30, 20, 10,
        60, 50, 40,
        0, 0,
    };

    var converted = PixelBufferConverter.PaddedBgr24ToRgb24(
        source,
        width: 2,
        height: 1,
        sourceStride: 8);

    SequenceEqual(
        new byte[] { 10, 20, 30, 40, 50, 60 },
        converted,
        "BGR to RGB conversion mismatch.");
}

static async Task StreamsScreenFrames()
{
    var capture = new StubScreenPixelCapture();
    var region = new ScreenRegion(-192, 20, 192, 108);
    var source = new ScreenFrameSource(
        capture,
        new ScreenFrameSourceOptions
        {
            Region = region,
            FramesPerSecond = 60,
        });

    var frames = await TakeAsync(source.ReadFramesAsync(), count: 3);

    Equal(3, frames.Count, "The screen source must produce the requested frames.");
    Equal(3, capture.InvocationCount, "The screen must be captured once per emitted frame.");
    Equal(region, capture.LastRegion, "The configured screen region was not preserved.");
    True(
        frames[0].ObservedAt <= frames[1].ObservedAt &&
        frames[1].ObservedAt <= frames[2].ObservedAt,
        "Screen frame timestamps must be monotonic.");
    True(
        frames.All(frame => frame.Frame.Width == 192 && frame.Frame.Height == 108),
        "Captured frame dimensions must match the configured region.");
}

static void SelectsUniquePanelMonitor()
{
    var primary = new DisplayMonitor(
        @"\\.\DISPLAY1",
        new ScreenRegion(0, 0, 192, 108),
        isPrimary: true);
    var panel = new DisplayMonitor(
        @"\\.\DISPLAY2",
        new ScreenRegion(-192, 0, 192, 108),
        isPrimary: false);
    var capture = new MappedScreenPixelCapture(new Dictionary<ScreenRegion, Rgb24Frame>
    {
        [primary.Bounds] = SolidFrame(192, 108, 30, 30, 30),
        [panel.Bounds] = DashboardFrame(),
    });
    var selector = new PanelMonitorSelector(
        new StubDisplayMonitorProvider(primary, panel),
        capture,
        TestLayoutRecognizer());

    var result = selector.Select();

    Equal(
        PanelMonitorSelectionStatus.Selected,
        result.Status,
        "Exactly one recognized panel must be selected.");
    var selectedMonitor = result.Monitor;
    NotNull(selectedMonitor, "A successful selection must expose its monitor.");
    Equal(panel, selectedMonitor, "The monitor containing the panel was not selected.");
    Equal(2, result.ScannedMonitorCount, "Every available monitor must be scanned.");
    Equal(1, result.MatchingMonitorCount, "Exactly one monitor must match.");
    SequenceEqual(
        new[] { primary.Bounds, panel.Bounds },
        capture.CapturedRegions,
        "Each monitor must be captured once in provider order.");
}

static void ReportsNoPanelMonitor()
{
    var first = new DisplayMonitor(
        @"\\.\DISPLAY1",
        new ScreenRegion(0, 0, 192, 108),
        isPrimary: true);
    var second = new DisplayMonitor(
        @"\\.\DISPLAY2",
        new ScreenRegion(192, 0, 192, 108),
        isPrimary: false);
    var capture = new MappedScreenPixelCapture(new Dictionary<ScreenRegion, Rgb24Frame>
    {
        [first.Bounds] = SolidFrame(192, 108, 30, 30, 30),
        [second.Bounds] = SolidFrame(192, 108, 245, 245, 245),
    });
    var selector = new PanelMonitorSelector(
        new StubDisplayMonitorProvider(first, second),
        capture,
        TestLayoutRecognizer());

    var result = selector.Select();

    Equal(
        PanelMonitorSelectionStatus.NoPanelFound,
        result.Status,
        "An unrecognized desktop must not be selected.");
    True(result.Monitor is null, "A failed selection must not expose a monitor.");
    Equal(2, result.ScannedMonitorCount, "Every available monitor must be scanned.");
    Equal(0, result.MatchingMonitorCount, "No monitor should match.");
}

static void RejectsAmbiguousPanelMonitors()
{
    var first = new DisplayMonitor(
        @"\\.\DISPLAY1",
        new ScreenRegion(0, 0, 192, 108),
        isPrimary: true);
    var second = new DisplayMonitor(
        @"\\.\DISPLAY2",
        new ScreenRegion(-192, 0, 192, 108),
        isPrimary: false);
    var capture = new MappedScreenPixelCapture(new Dictionary<ScreenRegion, Rgb24Frame>
    {
        [first.Bounds] = DashboardFrame(),
        [second.Bounds] = SyntheticFrame((22, 6, 0.90)),
    });
    var selector = new PanelMonitorSelector(
        new StubDisplayMonitorProvider(first, second),
        capture,
        TestLayoutRecognizer());

    var result = selector.Select();

    Equal(
        PanelMonitorSelectionStatus.MultiplePanelsFound,
        result.Status,
        "Multiple recognized panels must make the selection ambiguous.");
    True(result.Monitor is null, "An ambiguous selection must not choose a monitor.");
    Equal(2, result.MatchingMonitorCount, "Both recognized panels must be reported.");
}

static void ReportsNoAvailableMonitors()
{
    var capture = new MappedScreenPixelCapture(
        new Dictionary<ScreenRegion, Rgb24Frame>());
    var selector = new PanelMonitorSelector(
        new StubDisplayMonitorProvider(),
        capture,
        TestLayoutRecognizer());

    var result = selector.Select();

    Equal(
        PanelMonitorSelectionStatus.NoMonitorsAvailable,
        result.Status,
        "The absence of monitors must be explicit.");
    Equal(0, result.ScannedMonitorCount, "No monitor should be scanned.");
    Equal(0, capture.InvocationCount, "Screen capture must not run without monitors.");
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

static FrameSequenceSource StableCallSequence()
{
    var dashboard = DashboardFrame();
    var call = SyntheticFrame((22, 6, 0.90));

    return new FrameSequenceSource(new[]
    {
        new CapturedFrame(dashboard, TimeSpan.Zero),
        new CapturedFrame(call, Ms(1_000)),
        new CapturedFrame(call, Ms(1_300)),
        new CapturedFrame(call, Ms(1_600)),
    });
}

static async Task<List<T>> CollectAsync<T>(IAsyncEnumerable<T> source)
{
    var items = new List<T>();

    await foreach (var item in source)
    {
        items.Add(item);
    }

    return items;
}

static async Task<List<T>> TakeAsync<T>(IAsyncEnumerable<T> source, int count)
{
    var items = new List<T>(count);

    await foreach (var item in source)
    {
        items.Add(item);
        if (items.Count == count)
        {
            break;
        }
    }

    return items;
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

static void SequenceEqual<T>(
    IReadOnlyList<T> expected,
    IReadOnlyList<T> actual,
    string message)
{
    if (!expected.SequenceEqual(actual))
    {
        throw new InvalidOperationException(message);
    }
}

static void False(bool value, string message)
{
    if (value)
    {
        throw new InvalidOperationException(message);
    }
}

static void True(bool value, string message)
{
    if (!value)
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

sealed class StubScreenPixelCapture : IScreenPixelCapture
{
    public int InvocationCount { get; private set; }

    public ScreenRegion LastRegion { get; private set; }

    public Rgb24Frame Capture(ScreenRegion region)
    {
        InvocationCount++;
        LastRegion = region;
        var pixels = new byte[checked(region.Width * region.Height * 3)];
        Array.Fill(pixels, (byte)245);
        return new Rgb24Frame(region.Width, region.Height, pixels);
    }
}

sealed class StubDisplayMonitorProvider : IDisplayMonitorProvider
{
    private readonly IReadOnlyList<DisplayMonitor> _monitors;

    public StubDisplayMonitorProvider(params DisplayMonitor[] monitors)
    {
        _monitors = monitors;
    }

    public IReadOnlyList<DisplayMonitor> GetMonitors() => _monitors;
}

sealed class MappedScreenPixelCapture : IScreenPixelCapture
{
    private readonly IReadOnlyDictionary<ScreenRegion, Rgb24Frame> _frames;

    public MappedScreenPixelCapture(
        IReadOnlyDictionary<ScreenRegion, Rgb24Frame> frames)
    {
        _frames = frames;
    }

    public int InvocationCount => CapturedRegions.Count;

    public List<ScreenRegion> CapturedRegions { get; } = new();

    public Rgb24Frame Capture(ScreenRegion region)
    {
        CapturedRegions.Add(region);

        if (!_frames.TryGetValue(region, out var frame))
        {
            throw new InvalidOperationException(
                $"No synthetic frame was configured for {region}.");
        }

        return frame;
    }
}
