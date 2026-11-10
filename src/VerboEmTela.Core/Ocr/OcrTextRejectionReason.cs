namespace VerboEmTela.Core.Ocr;

public enum OcrTextRejectionReason
{
    None,
    Missing,
    TouchesBoundary,
    Ellipsis,
    InvalidCharacters,
}
