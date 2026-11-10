namespace VerboEmTela.Core.Ocr;

public sealed record OcrRegionRead
{
    public OcrRegionRead(string? text, double? confidence = null)
    {
        if (confidence is not null &&
            (double.IsNaN(confidence.Value) ||
             double.IsInfinity(confidence.Value) ||
             confidence.Value < 0 ||
             confidence.Value > 1))
        {
            throw new ArgumentOutOfRangeException(
                nameof(confidence),
                "OCR confidence must be between zero and one when supplied.");
        }

        Text = text;
        Confidence = confidence;
    }

    public string? Text { get; }

    public double? Confidence { get; }
}
