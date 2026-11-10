using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using VerboEmTela.Core.Application;
using VerboEmTela.Core.Detection;
using VerboEmTela.Core.Frames;
using VerboEmTela.Core.Imaging;
using VerboEmTela.Core.Layout;
using VerboEmTela.Core.Ocr;
using VerboEmTela.Core.Processing;
using VerboEmTela.Core.Specs.TestData;
using VerboEmTela.Windows.Application;
using VerboEmTela.Windows.Capture;
using VerboEmTela.Windows.Configuration;

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
    ("crops an RGB frame into independent memory", CropsFrameIntoIndependentMemory),
    ("rejects invalid frame crop regions", RejectsInvalidFrameRegions),
    ("extracts associated call regions from one captured frame", ExtractsAssociatedCallRegions),
    ("rejects a call region layout outside the frame", RejectsCallRegionLayoutOutsideFrame),
    ("converts RGB colors to deterministic luminance", ConvertsRgbToDeterministicLuminance),
    ("prepares associated call regions as luminance", PreparesAssociatedLuminanceRegions),
    ("reads each OCR field from its associated region", ReadsEachOcrFieldFromAssociatedRegion),
    ("rejects invalid OCR confidence", RejectsInvalidOcrConfidence),
    ("normalizes OCR whitespace while preserving accents", NormalizesOcrWhitespaceAndAccents),
    ("rejects incomplete or unsafe OCR text", RejectsIncompleteOrUnsafeOcrText),
    ("applies character rules for each OCR field", AppliesOcrFieldCharacterRules),
    ("does not expose rejected OCR field text", DoesNotExposeRejectedOcrFieldText),
    ("streams screen frames from an explicit region", () => StreamsScreenFrames().GetAwaiter().GetResult()),
    ("pauses capture until explicitly resumed", () => PausesFrameCapture().GetAwaiter().GetResult()),
    ("cancels capture while paused", () => CancelsPausedFrameCapture().GetAwaiter().GetResult()),
    ("selects the only monitor containing the panel", SelectsUniquePanelMonitor),
    ("reports when no monitor contains the panel", ReportsNoPanelMonitor),
    ("rejects an ambiguous panel monitor selection", RejectsAmbiguousPanelMonitors),
    ("reports when Windows exposes no monitors", ReportsNoAvailableMonitors),
    ("reports layout changes without repeating stable frames", () => ReportsOnlyLayoutChanges().GetAwaiter().GetResult()),
    ("refuses a second application instance", RefusesSecondApplicationInstance),
    ("uses safe monitoring configuration defaults", UsesSafeConfigurationDefaults),
    ("loads a valid monitoring configuration", LoadsValidMonitoringConfiguration),
    ("rejects unsafe or unknown configuration values", RejectsUnsafeMonitoringConfiguration),
    ("follows valid application state transitions", FollowsValidApplicationStateTransitions),
    ("rejects invalid application state transitions", RejectsInvalidApplicationStateTransitions),
    ("clears transient content on safety transitions", ClearsTransientContentOnSafetyTransitions),
    ("defines one hundred synthetic call cases", DefinesSyntheticCallMatrix),
    ("contains only explicitly fictitious identities", SyntheticMatrixContainsOnlyFictitiousIdentities),
    ("processes the complete synthetic call matrix", ProcessesSyntheticCallMatrix),
    ("validates privacy-safe video evidence metadata", ValidatesVideoEvidenceMetadata),
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
    Equal(
        Ms(1_600),
        extractor.LastObservedAt!.Value,
        "The extractor must receive the original frame timestamp.");
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

static void CropsFrameIntoIndependentMemory()
{
    const int width = 4;
    const int height = 3;
    var sourcePixels = new byte[width * height * 3];

    for (var y = 0; y < height; y++)
    {
        for (var x = 0; x < width; x++)
        {
            var pixel = ((y * width) + x) * 3;
            var value = (byte)((y * 10) + x);
            sourcePixels[pixel] = value;
            sourcePixels[pixel + 1] = (byte)(value + 100);
            sourcePixels[pixel + 2] = (byte)(value + 200);
        }
    }

    var frame = new Rgb24Frame(width, height, sourcePixels);
    var cropped = frame.Crop(new FrameRegion(left: 1, top: 1, width: 2, height: 2));
    var expected = new byte[]
    {
        11, 111, 211,
        12, 112, 212,
        21, 121, 221,
        22, 122, 222,
    };

    Equal(2, cropped.Width, "The cropped width is incorrect.");
    Equal(2, cropped.Height, "The cropped height is incorrect.");
    SequenceEqual(expected, cropped.PixelData.ToArray(), "The cropped pixels are incorrect.");

    Array.Fill(sourcePixels, (byte)0);
    SequenceEqual(
        expected,
        cropped.PixelData.ToArray(),
        "A crop must not retain the source frame buffer.");
}

static void RejectsInvalidFrameRegions()
{
    var frame = SolidFrame(4, 3, 10, 20, 30);

    Throws<ArgumentOutOfRangeException>(
        () => new FrameRegion(-1, 0, 1, 1),
        "A negative left coordinate must be rejected.");
    Throws<ArgumentOutOfRangeException>(
        () => new FrameRegion(0, -1, 1, 1),
        "A negative top coordinate must be rejected.");
    Throws<ArgumentOutOfRangeException>(
        () => new FrameRegion(0, 0, 0, 1),
        "A zero-width region must be rejected.");
    Throws<ArgumentOutOfRangeException>(
        () => new FrameRegion(0, 0, 1, 0),
        "A zero-height region must be rejected.");
    Throws<OverflowException>(
        () => new FrameRegion(int.MaxValue, 0, 1, 1),
        "A region whose right edge overflows must be rejected.");
    Throws<ArgumentException>(
        () => frame.Crop(default),
        "A default empty region must be rejected.");
    Throws<ArgumentException>(
        () => frame.Crop(new FrameRegion(3, 0, 2, 1)),
        "A region beyond the right edge must be rejected.");
    Throws<ArgumentException>(
        () => frame.Crop(new FrameRegion(0, 2, 1, 2)),
        "A region beyond the bottom edge must be rejected.");
}

static void ExtractsAssociatedCallRegions()
{
    const int width = 8;
    const int height = 4;
    var pixels = FilledPixels(width, height, 245, 245, 245);
    FillRectangle(pixels, width, 0, 0, 4, 2, 200, 10, 10);
    FillRectangle(pixels, width, 4, 0, 4, 2, 10, 200, 10);
    FillRectangle(pixels, width, 0, 2, 4, 2, 10, 10, 200);
    FillRectangle(pixels, width, 4, 2, 4, 2, 200, 200, 10);
    var capturedFrame = new CapturedFrame(
        new Rgb24Frame(width, height, pixels),
        Ms(1_234));
    var extractor = new CallFrameRegionExtractor(new CallRegionLayout(
        sector: new FrameRegion(0, 0, 4, 2),
        name: new FrameRegion(4, 0, 4, 2),
        password: new FrameRegion(0, 2, 4, 2),
        destination: new FrameRegion(4, 2, 4, 2)));

    var regions = extractor.Extract(capturedFrame);

    Equal(Ms(1_234), regions.ObservedAt, "The source frame timestamp must be preserved.");
    SequenceEqual(
        FilledPixels(4, 2, 200, 10, 10),
        regions.Sector.PixelData.ToArray(),
        "The sector region came from the wrong area.");
    SequenceEqual(
        FilledPixels(4, 2, 10, 200, 10),
        regions.Name.PixelData.ToArray(),
        "The name region came from the wrong area.");
    SequenceEqual(
        FilledPixels(4, 2, 10, 10, 200),
        regions.Password.PixelData.ToArray(),
        "The password region came from the wrong area.");
    SequenceEqual(
        FilledPixels(4, 2, 200, 200, 10),
        regions.Destination.PixelData.ToArray(),
        "The destination region came from the wrong area.");
}

static void RejectsCallRegionLayoutOutsideFrame()
{
    var validRegion = new FrameRegion(0, 0, 1, 1);

    Throws<ArgumentException>(
        () => new CallRegionLayout(
            default,
            validRegion,
            validRegion,
            validRegion),
        "A default unconfigured call region must be rejected.");

    var extractor = new CallFrameRegionExtractor(new CallRegionLayout(
        validRegion,
        validRegion,
        validRegion,
        new FrameRegion(3, 2, 2, 1)));
    var capturedFrame = new CapturedFrame(
        SolidFrame(4, 3, 245, 245, 245),
        TimeSpan.Zero);

    Throws<ArgumentException>(
        () => extractor.Extract(capturedFrame),
        "A call region outside the captured frame must reject the whole batch.");
}

static void ConvertsRgbToDeterministicLuminance()
{
    var frame = new Rgb24Frame(
        width: 5,
        height: 1,
        new byte[]
        {
            0, 0, 0,
            255, 255, 255,
            255, 0, 0,
            0, 255, 0,
            0, 0, 255,
        });

    var luminance = Rgb24LuminanceConverter.Convert(frame);

    Equal(5, luminance.Width, "The luminance width was not preserved.");
    Equal(1, luminance.Height, "The luminance height was not preserved.");
    SequenceEqual(
        new byte[] { 0, 255, 77, 149, 29 },
        luminance.PixelData.ToArray(),
        "RGB colors were converted with unexpected luminance weights.");
    Throws<ArgumentException>(
        () => new Luminance8Frame(2, 2, new byte[3]),
        "A luminance frame with incomplete pixels must be rejected.");
}

static void PreparesAssociatedLuminanceRegions()
{
    const int width = 4;
    const int height = 4;
    var pixels = FilledPixels(width, height, 0, 0, 0);
    FillRectangle(pixels, width, 0, 0, 2, 2, 255, 0, 0);
    FillRectangle(pixels, width, 2, 0, 2, 2, 0, 255, 0);
    FillRectangle(pixels, width, 0, 2, 2, 2, 0, 0, 255);
    FillRectangle(pixels, width, 2, 2, 2, 2, 255, 255, 255);
    var regions = new CallFrameRegionExtractor(new CallRegionLayout(
        sector: new FrameRegion(0, 0, 2, 2),
        name: new FrameRegion(2, 0, 2, 2),
        password: new FrameRegion(0, 2, 2, 2),
        destination: new FrameRegion(2, 2, 2, 2)))
        .Extract(new CapturedFrame(
            new Rgb24Frame(width, height, pixels),
            Ms(2_345)));

    var prepared = new CallRegionLuminancePreprocessor().Prepare(regions);

    Equal(Ms(2_345), prepared.ObservedAt, "Preprocessing must preserve the timestamp.");
    SequenceEqual(
        new byte[] { 77, 77, 77, 77 },
        prepared.Sector.PixelData.ToArray(),
        "The sector luminance came from the wrong region.");
    SequenceEqual(
        new byte[] { 149, 149, 149, 149 },
        prepared.Name.PixelData.ToArray(),
        "The name luminance came from the wrong region.");
    SequenceEqual(
        new byte[] { 29, 29, 29, 29 },
        prepared.Password.PixelData.ToArray(),
        "The password luminance came from the wrong region.");
    SequenceEqual(
        new byte[] { 255, 255, 255, 255 },
        prepared.Destination.PixelData.ToArray(),
        "The destination luminance came from the wrong region.");
}

static void ReadsEachOcrFieldFromAssociatedRegion()
{
    const int width = 4;
    const int height = 4;
    var pixels = FilledPixels(width, height, 0, 0, 0);
    FillRectangle(pixels, width, 0, 0, 2, 2, 255, 0, 0);
    FillRectangle(pixels, width, 2, 0, 2, 2, 0, 255, 0);
    FillRectangle(pixels, width, 0, 2, 2, 2, 0, 0, 255);
    FillRectangle(pixels, width, 2, 2, 2, 2, 255, 255, 255);
    var reader = new RecordingOcrRegionReader(new Dictionary<OcrFieldKind, OcrRegionRead>
    {
        [OcrFieldKind.Sector] = new OcrRegionRead("CLASSIFICAÇÃO", 0.91),
        [OcrFieldKind.Name] = new OcrRegionRead("  PESSOA FICTÍCIA\r\nTESTE  ", 0.88),
        [OcrFieldKind.Password] = new OcrRegionRead("TST0001", 0.94),
        [OcrFieldKind.Destination] = new OcrRegionRead("CONSULT. 03", 0.89),
    });
    var extractor = new CallRegionTextExtractor(
        new CallRegionLayout(
            sector: new FrameRegion(0, 0, 2, 2),
            name: new FrameRegion(2, 0, 2, 2),
            password: new FrameRegion(0, 2, 2, 2),
            destination: new FrameRegion(2, 2, 2, 2)),
        reader);

    var extracted = extractor.ExtractCall(new CapturedFrame(
        new Rgb24Frame(width, height, pixels),
        Ms(3_456)));

    Equal("CLASSIFICAÇÃO", extracted.Sector!, "The sector OCR text was not preserved.");
    Equal("PESSOA FICTÍCIA TESTE", extracted.Name!, "The name OCR text was not preserved.");
    Equal("TST0001", extracted.Password!, "The password OCR text was not preserved.");
    Equal("CONSULT. 03", extracted.Destination!, "The destination OCR text was not preserved.");
    Equal(4, reader.Invocations.Count, "Every OCR field must be read exactly once.");
    Equal(OcrFieldKind.Sector, reader.Invocations[0].Field, "Sector OCR order mismatch.");
    Equal((byte)77, reader.Invocations[0].FirstPixel, "Sector received the wrong region.");
    Equal(OcrFieldKind.Name, reader.Invocations[1].Field, "Name OCR order mismatch.");
    Equal((byte)149, reader.Invocations[1].FirstPixel, "Name received the wrong region.");
    Equal(OcrFieldKind.Password, reader.Invocations[2].Field, "Password OCR order mismatch.");
    Equal((byte)29, reader.Invocations[2].FirstPixel, "Password received the wrong region.");
    Equal(OcrFieldKind.Destination, reader.Invocations[3].Field, "Destination OCR order mismatch.");
    Equal((byte)255, reader.Invocations[3].FirstPixel, "Destination received the wrong region.");
}

static void RejectsInvalidOcrConfidence()
{
    var valid = new OcrRegionRead("texto bruto", 0.85);

    Equal("texto bruto", valid.Text!, "OCR text must remain unmodified.");
    Equal(0.85, valid.Confidence!.Value, "Valid OCR confidence was not preserved.");
    Throws<ArgumentOutOfRangeException>(
        () => new OcrRegionRead("texto", -0.01),
        "Negative OCR confidence must be rejected.");
    Throws<ArgumentOutOfRangeException>(
        () => new OcrRegionRead("texto", 1.01),
        "OCR confidence above one must be rejected.");
    Throws<ArgumentOutOfRangeException>(
        () => new OcrRegionRead("texto", double.NaN),
        "NaN OCR confidence must be rejected.");
}

static void NormalizesOcrWhitespaceAndAccents()
{
    var result = OcrTextNormalizer.Normalize(new OcrRegionRead(
        "  PESSOA\r\nFICTI\u0301CIA\t TESTE  ",
        confidence: 0.01));

    True(result.IsAccepted, "Valid low-confidence text must remain eligible for validation.");
    Equal(
        OcrTextRejectionReason.None,
        result.RejectionReason,
        "Valid text must not have a rejection reason.");
    Equal(
        "PESSOA FICTÍCIA TESTE",
        result.Text!,
        "Whitespace must collapse and decomposed accents must use Unicode Form C.");

    var abbreviation = OcrTextNormalizer.Normalize(
        new OcrRegionRead("CONSULT. 03"));
    True(abbreviation.IsAccepted, "A single abbreviation period must not be treated as ellipsis.");
    Equal("CONSULT. 03", abbreviation.Text!, "Valid punctuation must be preserved.");
}

static void RejectsIncompleteOrUnsafeOcrText()
{
    AssertOcrRejection(null, OcrTextRejectionReason.Missing);
    AssertOcrRejection("  \r\n\t  ", OcrTextRejectionReason.Missing);
    AssertOcrRejection("PESSOA TESTE...", OcrTextRejectionReason.Ellipsis);
    AssertOcrRejection("PESSOA TESTE…", OcrTextRejectionReason.Ellipsis);
    AssertOcrRejection("PESSOA\u0001TESTE", OcrTextRejectionReason.InvalidCharacters);
    AssertOcrRejection("PESSOA\u200BTESTE", OcrTextRejectionReason.InvalidCharacters);
    AssertOcrRejection("PESSOA\uFFFDTESTE", OcrTextRejectionReason.InvalidCharacters);

    var boundary = OcrTextNormalizer.Normalize(new OcrRegionRead(
        "PESSOA TESTE",
        confidence: 0.99,
        touchesBoundary: true));
    False(boundary.IsAccepted, "Text touching a crop boundary must be rejected.");
    Equal(
        OcrTextRejectionReason.TouchesBoundary,
        boundary.RejectionReason,
        "Boundary rejection reason mismatch.");
    True(boundary.Text is null, "Rejected OCR text must not escape validation.");
}

static void AppliesOcrFieldCharacterRules()
{
    var password = OcrTextNormalizer.Normalize(
        OcrFieldKind.Password,
        new OcrRegionRead("TST0001"));
    True(password.IsAccepted, "An alphanumeric password must be accepted.");

    var punctuatedPassword = OcrTextNormalizer.Normalize(
        OcrFieldKind.Password,
        new OcrRegionRead("TST-0001"));
    False(punctuatedPassword.IsAccepted, "Password punctuation must be rejected.");
    Equal(
        OcrTextRejectionReason.InvalidCharacters,
        punctuatedPassword.RejectionReason,
        "Password punctuation rejection reason mismatch.");

    var destination = OcrTextNormalizer.Normalize(
        OcrFieldKind.Destination,
        new OcrRegionRead("CONSULT. 03-A"));
    True(destination.IsAccepted, "Destination punctuation must remain available.");
    Equal(
        "CONSULT. 03-A",
        destination.Text!,
        "Valid destination punctuation must be preserved.");

    foreach (var field in Enum.GetValues<OcrFieldKind>())
    {
        var symbol = OcrTextNormalizer.Normalize(
            field,
            new OcrRegionRead("TESTE 🚨"));
        False(symbol.IsAccepted, $"Symbols must be rejected for {field}.");
        True(symbol.Text is null, $"Rejected {field} text must not escape validation.");
    }
}

static void DoesNotExposeRejectedOcrFieldText()
{
    var reader = new RecordingOcrRegionReader(new Dictionary<OcrFieldKind, OcrRegionRead>
    {
        [OcrFieldKind.Sector] = new OcrRegionRead("CLASSIFICAÇÃO"),
        [OcrFieldKind.Name] = new OcrRegionRead("PESSOA FICTÍCIA"),
        [OcrFieldKind.Password] = new OcrRegionRead("TST-0001"),
        [OcrFieldKind.Destination] = new OcrRegionRead("CONSULT. 03"),
    });
    var extractor = new CallRegionTextExtractor(
        new CallRegionLayout(
            sector: new FrameRegion(0, 0, 1, 1),
            name: new FrameRegion(1, 0, 1, 1),
            password: new FrameRegion(0, 1, 1, 1),
            destination: new FrameRegion(1, 1, 1, 1)),
        reader);

    var extracted = extractor.ExtractCall(new CapturedFrame(
        new Rgb24Frame(2, 2, FilledPixels(2, 2, 255, 255, 255)),
        Ms(4_000)));

    Equal("PESSOA FICTÍCIA", extracted.Name!, "Valid name text must be preserved.");
    True(extracted.Password is null, "Rejected password text must not leave the extractor.");
    Equal("CONSULT. 03", extracted.Destination!, "Valid destination must be preserved.");
}

static void AssertOcrRejection(
    string? text,
    OcrTextRejectionReason expectedReason)
{
    var result = OcrTextNormalizer.Normalize(new OcrRegionRead(text));

    False(result.IsAccepted, $"OCR text must be rejected as {expectedReason}.");
    Equal(expectedReason, result.RejectionReason, "OCR rejection reason mismatch.");
    True(result.Text is null, "Rejected OCR text must not expose normalized content.");
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

static async Task PausesFrameCapture()
{
    var capture = new StubScreenPixelCapture();
    var gate = new FrameCaptureGate();
    True(gate.Pause(), "The first pause request must close the capture gate.");
    False(gate.Pause(), "A repeated pause request must not change the gate.");

    var source = new ScreenFrameSource(
        capture,
        new ScreenFrameSourceOptions
        {
            Region = new ScreenRegion(0, 0, 192, 108),
            FramesPerSecond = 60,
        },
        captureGate: gate);
    await using var frames = source.ReadFramesAsync().GetAsyncEnumerator();

    var pendingFrame = frames.MoveNextAsync().AsTask();

    False(pendingFrame.IsCompleted, "A paused source must wait before capturing.");
    Equal(0, capture.InvocationCount, "A paused source must not capture a frame.");
    True(gate.IsPaused, "The capture gate must report its paused state.");

    True(gate.Resume(), "Resume must reopen the capture gate.");
    True(await pendingFrame, "The source must emit a frame after resume.");
    Equal(1, capture.InvocationCount, "Resume must allow exactly one pending capture.");
    False(gate.IsPaused, "The capture gate must report its resumed state.");
    False(gate.Resume(), "A repeated resume request must not change the gate.");
}

static async Task CancelsPausedFrameCapture()
{
    var capture = new StubScreenPixelCapture();
    var gate = new FrameCaptureGate();
    gate.Pause();
    using var cancellation = new CancellationTokenSource();
    var source = new ScreenFrameSource(
        capture,
        new ScreenFrameSourceOptions
        {
            Region = new ScreenRegion(0, 0, 192, 108),
            FramesPerSecond = 60,
        },
        captureGate: gate);
    await using var frames = source
        .ReadFramesAsync(cancellation.Token)
        .GetAsyncEnumerator();
    var pendingFrame = frames.MoveNextAsync().AsTask();

    cancellation.Cancel();
    var cancellationObserved = false;

    try
    {
        await pendingFrame;
    }
    catch (OperationCanceledException)
    {
        cancellationObserved = true;
    }

    True(cancellationObserved, "Cancellation must unblock a paused capture source.");
    Equal(0, capture.InvocationCount, "Cancellation while paused must not capture a frame.");
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

static async Task ReportsOnlyLayoutChanges()
{
    var dashboard = DashboardFrame();
    var call = SyntheticFrame((22, 6, 0.90));
    var darkInvalid = SolidFrame(192, 108, 30, 30, 30);
    var lightInvalid = SolidFrame(192, 108, 245, 245, 245);
    var source = new FrameSequenceSource(new[]
    {
        new CapturedFrame(dashboard, Ms(0)),
        new CapturedFrame(dashboard, Ms(200)),
        new CapturedFrame(call, Ms(400)),
        new CapturedFrame(call, Ms(600)),
        new CapturedFrame(darkInvalid, Ms(800)),
        new CapturedFrame(lightInvalid, Ms(1_000)),
    });
    var runner = new LayoutMonitoringRunner(TestLayoutRecognizer());

    var events = await CollectAsync(runner.RunAsync(source));

    Equal(4, events.Count, "Only layout state changes must be reported.");
    Equal(PanelMode.Dashboard, events[0].Mode, "The initial dashboard was not reported.");
    Equal(Ms(0), events[0].ObservedAt, "The initial timestamp was not preserved.");
    Equal(PanelMode.FullScreenCall, events[1].Mode, "The call transition was not reported.");
    Equal(Ms(400), events[1].ObservedAt, "The call timestamp was not preserved.");
    Equal(PanelMode.Invalid, events[2].Mode, "The invalid transition was not reported.");
    Equal(
        LayoutRejectionReason.InsufficientLightBackground,
        events[2].RejectionReason,
        "The first invalid reason was not preserved.");
    Equal(PanelMode.Invalid, events[3].Mode, "A changed invalid reason must be reported.");
    Equal(
        LayoutRejectionReason.UnexpectedGreenBandCount,
        events[3].RejectionReason,
        "The changed invalid reason was not preserved.");
}

static void RefusesSecondApplicationInstance()
{
    var instanceName = $@"Local\VerboEmTela.Specs.{Guid.NewGuid():N}";
    var first = SingleInstanceLease.TryAcquire(instanceName);

    NotNull(first, "The first application instance must acquire the lease.");

    try
    {
        using var second = SingleInstanceLease.TryAcquire(instanceName);
        True(second is null, "A second application instance must be refused.");
    }
    finally
    {
        first.Dispose();
    }

    using var reacquired = SingleInstanceLease.TryAcquire(instanceName);
    NotNull(reacquired, "The lease must be available after the first instance exits.");
}

static void UsesSafeConfigurationDefaults()
{
    var configuration = new MonitoringConfigurationLoader().Parse("{}");

    Equal(1, configuration.SchemaVersion, "The safe schema version was not applied.");
    Equal(1920, configuration.RequiredWidth, "The safe width was not applied.");
    Equal(1080, configuration.RequiredHeight, "The safe height was not applied.");
    Equal(5d, configuration.FramesPerSecond, "The safe frame rate was not applied.");
}

static void LoadsValidMonitoringConfiguration()
{
    var configuration = new MonitoringConfigurationLoader().Parse(
        """
        {
          "schemaVersion": 1,
          "requiredWidth": 1920,
          "requiredHeight": 1080,
          "framesPerSecond": 8
        }
        """);

    Equal(8d, configuration.FramesPerSecond, "The configured frame rate was not loaded.");
}

static void RejectsUnsafeMonitoringConfiguration()
{
    var loader = new MonitoringConfigurationLoader();

    Throws<ConfigurationException>(
        () => loader.Parse("""{ "schemaVersion": 2 }"""),
        "An incompatible schema version must be rejected.");
    Throws<ConfigurationException>(
        () => loader.Parse("""{ "requiredWidth": 1280, "requiredHeight": 720 }"""),
        "An unvalidated resolution must be rejected.");
    Throws<ConfigurationException>(
        () => loader.Parse("""{ "framesPerSecond": 4 }"""),
        "A frame rate below the safe range must be rejected.");
    Throws<ConfigurationException>(
        () => loader.Parse("""{ "framesPerSecond": 11 }"""),
        "A frame rate above the safe range must be rejected.");
    Throws<ConfigurationException>(
        () => loader.Parse("""{ "framesPerSecond": 5, "framePerSecond": 6 }"""),
        "An unknown property must be rejected.");
    Throws<ConfigurationException>(
        () => loader.Parse("{ invalid json"),
        "Malformed JSON must be rejected.");
}

static void FollowsValidApplicationStateTransitions()
{
    var machine = new ApplicationStateMachine(new CountingContentCleaner());

    Equal(ApplicationState.Starting, machine.Current.State, "The initial state must be Starting.");
    Equal(ApplicationState.Arming, machine.CompleteStartup().State, "Startup must enter Arming.");
    Equal(
        ApplicationState.Monitoring,
        machine.ConfirmPanelReady().State,
        "A ready panel must enter Monitoring.");
    Equal(ApplicationState.Speaking, machine.BeginSpeaking().State, "Speech must enter Speaking.");
    Equal(
        ApplicationState.Monitoring,
        machine.CompleteSpeaking().State,
        "Completed speech must return to Monitoring.");
    Equal(ApplicationState.Paused, machine.Pause().State, "Pause must enter Paused.");
    Equal(ApplicationState.Arming, machine.Resume().State, "Resume must re-enter Arming.");
    machine.ConfirmPanelReady();
    Equal(
        ApplicationState.Arming,
        machine.ReportPanelLost().State,
        "Panel loss must re-enter Arming.");

    var failure = machine.Fail("  dispositivo de áudio indisponível  ");
    Equal(ApplicationState.Error, failure.State, "Failure must enter Error.");
    Equal(
        "dispositivo de áudio indisponível",
        failure.ErrorReason!,
        "The current error reason must be trimmed and exposed.");
    var recovered = machine.Recover();
    Equal(ApplicationState.Arming, recovered.State, "Recovery must re-enter Arming.");
    True(recovered.ErrorReason is null, "Recovery must clear the current error reason.");
    Equal(ApplicationState.Stopped, machine.Stop().State, "Stop must be terminal.");
}

static void RejectsInvalidApplicationStateTransitions()
{
    var machine = new ApplicationStateMachine(new CountingContentCleaner());

    Throws<InvalidOperationException>(
        () => machine.ConfirmPanelReady(),
        "Starting must not jump directly to Monitoring.");
    Throws<InvalidOperationException>(
        () => machine.BeginSpeaking(),
        "Starting must not jump directly to Speaking.");

    machine.CompleteStartup();
    Throws<InvalidOperationException>(
        () => machine.CompleteSpeaking(),
        "Arming must not complete speech.");

    machine.Stop();
    Throws<InvalidOperationException>(
        () => machine.Resume(),
        "A stopped application must not resume.");
    Throws<InvalidOperationException>(
        () => machine.Fail("late failure"),
        "A stopped application must remain terminal.");
    Throws<ArgumentException>(
        () => new ApplicationStateMachine(new CountingContentCleaner()).Fail("  "),
        "An empty error reason must be rejected.");
}

static void ClearsTransientContentOnSafetyTransitions()
{
    var cleaner = new CountingContentCleaner();
    var machine = new ApplicationStateMachine(cleaner);

    machine.CompleteStartup();
    machine.ConfirmPanelReady();
    machine.BeginSpeaking();
    machine.CompleteSpeaking();
    Equal(0, cleaner.InvocationCount, "Ordinary progress must not clear transient content.");

    machine.Pause();
    machine.Resume();
    machine.ConfirmPanelReady();
    machine.ReportPanelLost();
    machine.Fail("capture unavailable");
    machine.Recover();
    machine.Stop();

    Equal(
        6,
        cleaner.InvocationCount,
        "Pause, resume, panel loss, error, recovery and stop must each clear content.");
}

static void DefinesSyntheticCallMatrix()
{
    var matrix = SyntheticCallMatrix.Create();

    Equal(100, matrix.Count, "The synthetic matrix must contain exactly 100 cases.");
    Equal(
        100,
        matrix.Select(testCase => testCase.Id).Distinct().Count(),
        "Every synthetic case must have a unique identifier.");

    foreach (var kind in Enum.GetValues<SyntheticEventKind>())
    {
        Equal(
            10,
            matrix.Count(testCase => testCase.Kind == kind),
            $"Synthetic category {kind} must contain ten cases.");
    }

    Equal(
        80,
        matrix.Count(testCase =>
            testCase.ExpectedOutcome is SyntheticExpectedOutcome.Detect),
        "The matrix must contain 80 complete detectable cases.");
    Equal(
        20,
        matrix.Count(testCase =>
            testCase.ExpectedOutcome is SyntheticExpectedOutcome.Reject),
        "The matrix must contain 20 intentionally rejected cases.");
    SequenceEqual(
        new[] { "CLASSIFICAÇÃO", "ODONTOLOGIA", "RECEPÇÃO" },
        matrix
            .Where(testCase => testCase.Sector is not null)
            .Select(testCase => testCase.Sector!)
            .Distinct()
            .Order()
            .ToArray(),
        "The matrix must cover all three panel sectors.");
}

static void SyntheticMatrixContainsOnlyFictitiousIdentities()
{
    var matrix = SyntheticCallMatrix.Create();

    True(
        matrix.All(testCase =>
            testCase.Name is null ||
            testCase.Name.StartsWith(
                "PESSOA FICTÍCIA",
                StringComparison.Ordinal)),
        "Every synthetic name must be explicitly marked as fictitious.");
    True(
        matrix.All(testCase =>
            testCase.Password is null ||
            testCase.Password.StartsWith("TST", StringComparison.Ordinal)),
        "Every synthetic password must use the reserved TST prefix.");
}

static void ProcessesSyntheticCallMatrix()
{
    foreach (var testCase in SyntheticCallMatrix.Create())
    {
        var extractor = new StubTextExtractor(new ExtractedCallText(
            testCase.Sector,
            testCase.Name,
            testCase.Password,
            testCase.Destination));
        var processor = TestFrameProcessor(extractor);
        processor.Process(DashboardFrame(), TimeSpan.Zero);
        DetectedCall? detected;

        switch (testCase.Kind)
        {
            case SyntheticEventKind.InvalidScreen:
                var invalid = processor.Process(
                    SolidFrame(192, 108, 30, 30, 30),
                    Ms(1_000));
                detected = invalid.DetectedCall;
                Equal(
                    FrameProcessingStatus.InvalidLayout,
                    invalid.Status,
                    $"{testCase.Id} must be rejected as an invalid screen.");
                Equal(
                    0,
                    extractor.InvocationCount,
                    $"{testCase.Id} must not invoke text extraction.");
                break;

            case SyntheticEventKind.RecallAfterDashboard:
                NotNull(
                    ProcessStableFrameCall(processor, 1_000),
                    $"{testCase.Id} setup call was not detected.");
                processor.Process(DashboardFrame(), Ms(2_000));
                detected = ProcessStableFrameCall(processor, 3_000);
                break;

            case SyntheticEventKind.RapidConsecutiveCall:
                extractor.Result = new ExtractedCallText(
                    testCase.Sector,
                    $"PESSOA FICTÍCIA ANTERIOR {testCase.Id}",
                    null,
                    "CONSULTÓRIO 9");
                NotNull(
                    ProcessStableFrameCall(processor, 1_000),
                    $"{testCase.Id} preceding call was not detected.");
                extractor.Result = new ExtractedCallText(
                    testCase.Sector,
                    testCase.Name,
                    testCase.Password,
                    testCase.Destination);
                detected = ProcessStableFrameCall(processor, 2_000);
                break;

            default:
                detected = ProcessStableFrameCall(processor, 1_000);
                break;
        }

        if (testCase.ExpectedOutcome is SyntheticExpectedOutcome.Reject)
        {
            True(
                detected is null,
                $"{testCase.Id} must not produce a detected call.");
            continue;
        }

        NotNull(detected, $"{testCase.Id} must produce one detected call.");
        Equal(testCase.Sector!, detected.Sector, $"{testCase.Id} sector mismatch.");
        Equal(
            testCase.ExpectedIdentityKind!.Value,
            detected.IdentityKind,
            $"{testCase.Id} identity kind mismatch.");
        Equal(
            testCase.ExpectedIdentity!,
            detected.Identity,
            $"{testCase.Id} identity mismatch.");
        Equal(
            testCase.Destination!,
            detected.Destination,
            $"{testCase.Id} destination mismatch.");
    }
}

static void ValidatesVideoEvidenceMetadata()
{
    var evidencePath = Path.Combine(
        AppContext.BaseDirectory,
        "Evidence",
        "video-evidence.v1.json");
    var manifest = JsonSerializer.Deserialize<VideoEvidenceManifest>(
        File.ReadAllText(evidencePath),
        new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        });

    NotNull(manifest, "The video evidence manifest must be readable.");
    Equal(1, manifest.SchemaVersion, "The evidence schema version is incompatible.");
    Equal("EV-001", manifest.EvidenceId, "The evidence identifier changed unexpectedly.");
    True(
        manifest.Sha256.Length == 64 && manifest.Sha256.All(Uri.IsHexDigit),
        "The evidence hash must be a complete SHA-256 value.");
    Equal(3_574_090L, manifest.SizeBytes, "The recorded video size changed unexpectedly.");
    Equal(60d, manifest.DurationSeconds, "The recorded duration is incorrect.");
    Equal(1920, manifest.Video.Width, "The recorded video width is incorrect.");
    Equal(1080, manifest.Video.Height, "The recorded video height is incorrect.");
    Equal(60d, manifest.Video.FramesPerSecond, "The recorded frame rate is incorrect.");
    Equal(444, manifest.Video.DataRateKbps, "The recorded video data rate is incorrect.");
    Equal(
        "{34363248-0000-0010-8000-00AA00389B71}",
        manifest.Video.CodecIdentifier,
        "The raw video codec identifier changed unexpectedly.");
    True(manifest.Audio.Present, "The Windows metadata indicates an audio track.");
    Equal(14, manifest.Audio.BitRateKbps, "The recorded audio bit rate is incorrect.");
    False(
        manifest.Audio.CodecIdentified,
        "The audio codec must remain explicitly unidentified until verified.");
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

    public TimeSpan? LastObservedAt { get; private set; }

    public ExtractedCallText ExtractCall(CapturedFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        InvocationCount++;
        LastObservedAt = frame.ObservedAt;
        return Result;
    }
}

sealed record RecordedOcrInvocation(
    OcrFieldKind Field,
    int Width,
    int Height,
    byte FirstPixel);

sealed class RecordingOcrRegionReader : IOcrRegionReader
{
    private readonly IReadOnlyDictionary<OcrFieldKind, OcrRegionRead> _results;

    public RecordingOcrRegionReader(
        IReadOnlyDictionary<OcrFieldKind, OcrRegionRead> results)
    {
        _results = results;
    }

    public List<RecordedOcrInvocation> Invocations { get; } = new();

    public OcrRegionRead Read(
        OcrFieldKind field,
        Luminance8Frame frame)
    {
        Invocations.Add(new RecordedOcrInvocation(
            field,
            frame.Width,
            frame.Height,
            frame.PixelData.Span[0]));

        return _results[field];
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

sealed class CountingContentCleaner : ITransientContentCleaner
{
    public int InvocationCount { get; private set; }

    public void Clear()
    {
        InvocationCount++;
    }
}
