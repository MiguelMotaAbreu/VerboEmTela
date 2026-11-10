using System.Globalization;
using System.Text;

namespace VerboEmTela.Core.Ocr;

public static class OcrTextNormalizer
{
    public static OcrTextValidationResult Normalize(OcrRegionRead read)
    {
        ArgumentNullException.ThrowIfNull(read);

        if (read.TouchesBoundary)
        {
            return Rejected(OcrTextRejectionReason.TouchesBoundary);
        }

        if (string.IsNullOrWhiteSpace(read.Text))
        {
            return Rejected(OcrTextRejectionReason.Missing);
        }

        string normalized;

        try
        {
            normalized = CollapseWhitespace(
                read.Text.Normalize(NormalizationForm.FormC));
        }
        catch (ArgumentException)
        {
            return Rejected(OcrTextRejectionReason.InvalidCharacters);
        }

        if (normalized.Length == 0)
        {
            return Rejected(OcrTextRejectionReason.Missing);
        }

        if (normalized.Contains("...", StringComparison.Ordinal) ||
            normalized.Contains('…'))
        {
            return Rejected(OcrTextRejectionReason.Ellipsis);
        }

        foreach (var rune in normalized.EnumerateRunes())
        {
            if (rune.Value == 0xFFFD || IsRejectedCategory(
                Rune.GetUnicodeCategory(rune)))
            {
                return Rejected(OcrTextRejectionReason.InvalidCharacters);
            }
        }

        return new OcrTextValidationResult(
            normalized,
            OcrTextRejectionReason.None);
    }

    private static string CollapseWhitespace(string value)
    {
        var builder = new StringBuilder(value.Length);
        var pendingSpace = false;

        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    private static bool IsRejectedCategory(UnicodeCategory category)
    {
        return category is
            UnicodeCategory.Control or
            UnicodeCategory.Format or
            UnicodeCategory.PrivateUse or
            UnicodeCategory.OtherNotAssigned or
            UnicodeCategory.Surrogate;
    }

    private static OcrTextValidationResult Rejected(
        OcrTextRejectionReason reason)
    {
        return new OcrTextValidationResult(null, reason);
    }
}
