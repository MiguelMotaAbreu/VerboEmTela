using VerboEmTela.Core.Detection;
using VerboEmTela.Core.Frames;
using VerboEmTela.Core.Imaging;
using VerboEmTela.Core.Layout;

namespace VerboEmTela.Core.Processing;

public sealed class CallFrameProcessor
{
    private readonly IPanelLayoutRecognizer _layoutRecognizer;
    private readonly IFrameTextExtractor _textExtractor;
    private readonly CallDetector _callDetector;
    private readonly CallFrameProcessorOptions _options;
    private TimeSpan? _lastFrameTime;
    private TimeSpan? _invalidLayoutSince;
    private bool _invalidLayoutConfirmed;

    public CallFrameProcessor(
        IPanelLayoutRecognizer layoutRecognizer,
        IFrameTextExtractor textExtractor,
        CallDetector? callDetector = null,
        CallFrameProcessorOptions? options = null)
    {
        _layoutRecognizer = layoutRecognizer ?? throw new ArgumentNullException(nameof(layoutRecognizer));
        _textExtractor = textExtractor ?? throw new ArgumentNullException(nameof(textExtractor));
        _callDetector = callDetector ?? new CallDetector();
        _options = options ?? new CallFrameProcessorOptions();
        _options.Validate();
    }

    public FrameProcessingResult Process(Rgb24Frame frame, TimeSpan observedAt)
    {
        ArgumentNullException.ThrowIfNull(frame);
        EnsureMonotonicTime(observedAt);

        var layout = _layoutRecognizer.Recognize(frame);

        if (layout.Mode is PanelMode.Invalid)
        {
            return HandleInvalidLayout(layout, observedAt);
        }

        ConfirmExpiredInvalidLayout(observedAt);
        ClearInvalidLayoutTransition();

        if (layout.Mode is PanelMode.Dashboard)
        {
            _callDetector.Process(PanelObservation.Dashboard(observedAt));
            return Result(layout, FrameProcessingStatus.DashboardObserved);
        }

        if (!_callDetector.IsArmed)
        {
            return Result(layout, FrameProcessingStatus.IgnoredUntilDashboard);
        }

        var extraction = _textExtractor.ExtractCall(
            new CapturedFrame(frame, observedAt));
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
        ClearInvalidLayoutTransition();
        _callDetector.Reset();
    }

    private FrameProcessingResult HandleInvalidLayout(
        LayoutRecognition layout,
        TimeSpan observedAt)
    {
        _invalidLayoutSince ??= observedAt;

        if (!_invalidLayoutConfirmed &&
            observedAt - _invalidLayoutSince.Value >=
                _options.InvalidLayoutGracePeriod)
        {
            _callDetector.Process(PanelObservation.Invalid(observedAt));
            _invalidLayoutConfirmed = true;
        }

        return Result(
            layout,
            _invalidLayoutConfirmed
                ? FrameProcessingStatus.InvalidLayout
                : FrameProcessingStatus.LayoutTransitionObserved);
    }

    private void ConfirmExpiredInvalidLayout(TimeSpan observedAt)
    {
        if (!_invalidLayoutConfirmed &&
            _invalidLayoutSince is not null &&
            observedAt - _invalidLayoutSince.Value >=
                _options.InvalidLayoutGracePeriod)
        {
            _callDetector.Process(PanelObservation.Invalid(observedAt));
            _invalidLayoutConfirmed = true;
        }
    }

    private void ClearInvalidLayoutTransition()
    {
        _invalidLayoutSince = null;
        _invalidLayoutConfirmed = false;
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
