namespace VerboEmTela.Core.Ocr;

public sealed record OcrTextValidationResult
{
    internal OcrTextValidationResult(
        string? text,
        OcrTextRejectionReason rejectionReason)
    {
        Text = text;
        RejectionReason = rejectionReason;
    }

    public string? Text { get; }

    public OcrTextRejectionReason RejectionReason { get; }

    public bool IsAccepted => RejectionReason is OcrTextRejectionReason.None;
}
