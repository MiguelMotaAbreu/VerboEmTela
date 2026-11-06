using System.Runtime.CompilerServices;
using VerboEmTela.Core.Frames;

namespace VerboEmTela.Core.Layout;

public sealed class LayoutMonitoringRunner
{
    private readonly IPanelLayoutRecognizer _layoutRecognizer;

    public LayoutMonitoringRunner(IPanelLayoutRecognizer layoutRecognizer)
    {
        _layoutRecognizer = layoutRecognizer ??
            throw new ArgumentNullException(nameof(layoutRecognizer));
    }

    public async IAsyncEnumerable<LayoutMonitoringEvent> RunAsync(
        IFrameSource source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        LayoutMonitoringEvent? previous = null;

        await foreach (var capturedFrame in source
            .ReadFramesAsync(cancellationToken)
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            var recognition = _layoutRecognizer.Recognize(capturedFrame.Frame);

            if (previous is not null &&
                previous.Mode == recognition.Mode &&
                previous.RejectionReason == recognition.RejectionReason)
            {
                continue;
            }

            previous = new LayoutMonitoringEvent(
                capturedFrame.ObservedAt,
                recognition.Mode,
                recognition.RejectionReason);

            yield return previous;
        }
    }
}
