using VerboEmTela.Core.Detection;
using VerboEmTela.Core.Imaging;
using VerboEmTela.Core.Layout;

namespace VerboEmTela.Core.Processing;

public sealed class CallFrameProcessor
{
    private readonly IPanelLayoutRecognizer _layoutRecognizer;
    private readonly IFrameTextExtractor _textExtractor;
    private readonly CallDetector _callDetector;
    private TimeSpan? _lastFrameTime;

    public CallFrameProcessor(
        IPanelLayoutRecognizer layoutRecognizer,
        IFrameTextExtractor textExtractor,
        CallDetector? callDetector = null)
    {
        _layoutRecognizer = layoutRecognizer ?? throw new ArgumentNullException(nameof(layoutRecognizer));
        _textExtractor = textExtractor ?? throw new ArgumentNullException(nameof(textExtractor));
        _callDetector = callDetector ?? new CallDetector();
    }

    public FrameProcessingResult Process(Rgb24Frame frame, TimeSpan observedAt)
    {
        ArgumentNullException.ThrowIfNull(frame);
        EnsureMonotonicTime(observedAt);

        var layout = _layoutRecognizer.Recognize(frame);

        if (layout.Mode is PanelMode.Invalid)
        {
            _callDetector.Process(PanelObservation.Invalid(observedAt));
            return Result(layout, FrameProcessingStatus.InvalidLayout);
        }

        if (layout.Mode is PanelMode.Dashboard)
        {
            _callDetector.Process(PanelObservation.Dashboard(observedAt));
            return Result(layout, FrameProcessingStatus.DashboardObserved);
        }

        if (!_callDetector.IsArmed)
        {
            return Result(layout, FrameProcessingStatus.IgnoredUntilDashboard);
        }

        var extraction = _textExtractor.ExtractCall(frame);
        var sector = Clean(extraction.Sector);
        var destination = Clean(extraction.Destination);
        var name = Clean(extraction.Name);
        var password = Clean(extraction.Password);

        if (sector is null || destination is null || (name is null && password is null))
        {
            return Result(layout, FrameProcessingStatus.IncompleteCall);
        }

        var identityKind = name is not null
            ? IdentityKind.Name
            : IdentityKind.Password;
        var identity = name ?? password!;

        var detectedCall = _callDetector.Process(
            PanelObservation.FullScreenCall(
                observedAt,
                sector,
                identityKind,
                identity,
                destination));

        return new FrameProcessingResult(
            layout.Mode,
            detectedCall is null
                ? FrameProcessingStatus.CallObserved
                : FrameProcessingStatus.CallDetected,
            layout.RejectionReason,
            detectedCall);
    }

    public void Reset()
    {
        _lastFrameTime = null;
        _callDetector.Reset();
    }

    private static FrameProcessingResult Result(
        LayoutRecognition layout,
        FrameProcessingStatus status)
    {
        return new FrameProcessingResult(
            layout.Mode,
            status,
            layout.RejectionReason,
            null);
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private void EnsureMonotonicTime(TimeSpan observedAt)
    {
        if (observedAt < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(observedAt));
        }

        if (_lastFrameTime is not null && observedAt < _lastFrameTime)
        {
            throw new ArgumentException(
                "Frames must be processed in chronological order.",
                nameof(observedAt));
        }

        _lastFrameTime = observedAt;
    }
}
