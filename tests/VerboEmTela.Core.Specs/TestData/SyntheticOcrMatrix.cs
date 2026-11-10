using VerboEmTela.Core.Ocr;

namespace VerboEmTela.Core.Specs.TestData;

internal sealed record SyntheticOcrCase(
    string Id,
    OcrFieldKind Field,
    string RenderedText,
    string ExpectedText,
    int Width,
    int Height,
    double FontSize);

internal static class SyntheticOcrMatrix
{
    public static IReadOnlyList<SyntheticOcrCase> Create()
    {
        return new[]
        {
            new SyntheticOcrCase(
                "OCR-001",
                OcrFieldKind.Sector,
                "CLASSIFICAÇÃO",
                "CLASSIFICAÇÃO",
                760,
                120,
                58),
            new SyntheticOcrCase(
                "OCR-002",
                OcrFieldKind.Name,
                "PESSOA FICTÍCIA",
                "PESSOA FICTÍCIA",
                820,
                120,
                58),
            new SyntheticOcrCase(
                "OCR-003",
                OcrFieldKind.Name,
                "PESSOA FICTÍCIA\nDA SILVA TESTE",
                "PESSOA FICTÍCIA DA SILVA TESTE",
                920,
                190,
                52),
            new SyntheticOcrCase(
                "OCR-004",
                OcrFieldKind.Name,
                "PESSOA FICTÍCIA MARIA LUIZA TESTE",
                "PESSOA FICTÍCIA MARIA LUIZA TESTE",
                1_180,
                120,
                48),
            new SyntheticOcrCase(
                "OCR-005",
                OcrFieldKind.Password,
                "TST0001",
                "TST0001",
                520,
                120,
                64),
            new SyntheticOcrCase(
                "OCR-006",
                OcrFieldKind.Destination,
                "CONSULTÓRIO 03",
                "CONSULTÓRIO 03",
                720,
                120,
                58),
            new SyntheticOcrCase(
                "OCR-007",
                OcrFieldKind.Destination,
                "RECEPÇÃO 3",
                "RECEPÇÃO 3",
                620,
                120,
                58),
            new SyntheticOcrCase(
                "OCR-008",
                OcrFieldKind.Destination,
                "ODONTOLOGIA",
                "ODONTOLOGIA",
                700,
                120,
                58),
            new SyntheticOcrCase(
                "OCR-009",
                OcrFieldKind.Destination,
                "GUICHÊ 03",
                "GUICHÊ 03",
                600,
                120,
                58),
        };
    }
}
