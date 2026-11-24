using VerboEmTela.Core.Imaging;
using VerboEmTela.Core.Ocr;

namespace VerboEmTela.Ocr.Tesseract;

public sealed class TesseractOcrRegionReader : IOcrRegionReader, IDisposable
{
    private const int BoundarySafetyMarginPixels = 1;
    private const int MaximumBlankLuminanceRange = 20;
    private const float MinimumPasswordConfidence = 0.50f;

    private readonly global::Tesseract.TesseractEngine _fastEngine;
    private readonly global::Tesseract.TesseractEngine _linguisticEngine;
    private readonly object _sync = new();
    private bool _disposed;

    public TesseractOcrRegionReader(
        string fastTessdataPath,
        string linguisticTessdataPath)
    {
        if (!string.IsNullOrEmpty(
            Environment.GetEnvironmentVariable("TESSDATA_PREFIX")))
        {
            throw new InvalidOperationException(
                "TESSDATA_PREFIX must be unset so the packaged OCR model cannot be overridden.");
        }

        _fastEngine = CreateEngine(fastTessdataPath);

        try
        {
            _linguisticEngine = CreateEngine(linguisticTessdataPath);
        }
        catch
        {
            _fastEngine.Dispose();
            throw;
        }
    }

    private static global::Tesseract.TesseractEngine CreateEngine(
        string tessdataPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tessdataPath);

        var fullDataPath = Path.GetFullPath(tessdataPath);
        var modelPath = Path.Combine(
            fullDataPath,
            "por.traineddata");

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException(
                "The packaged Portuguese OCR model was not found.",
                modelPath);
        }

        return new global::Tesseract.TesseractEngine(
            fullDataPath,
            "por",
            global::Tesseract.EngineMode.LstmOnly);
    }

    public OcrRegionRead Read(
        OcrFieldKind field,
        Luminance8Frame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (HasNoTrustworthyContrast(frame))
            {
                return new OcrRegionRead(null);
            }

            using var image = global::Tesseract.Pix.LoadFromMemory(
                GrayscaleBmpEncoder.Encode(frame));

            if (field is OcrFieldKind.Name)
            {
                return ReadNameWithMultilineConsensus(image, frame);
            }

            if (field is OcrFieldKind.Destination)
            {
                return ReadDestinationWithCompactCodeConsensus(image, frame);
            }

            using var page = EngineFor(field).Process(
                image,
                PageSegmentationFor(field));
            var text = page.GetText();
            var confidence = page.GetMeanConfidence();

            if (field is OcrFieldKind.Password &&
                confidence < MinimumPasswordConfidence)
            {
                return new OcrRegionRead(null, confidence);
            }

            return new OcrRegionRead(
                text,
                confidence,
                !string.IsNullOrWhiteSpace(text) &&
                TouchesBoundary(page, frame));
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                _linguisticEngine.Dispose();
            }
            finally
            {
                _fastEngine.Dispose();
            }

            _disposed = true;
        }
    }

    private global::Tesseract.TesseractEngine EngineFor(OcrFieldKind field)
    {
        return field is OcrFieldKind.Password
            ? _fastEngine
            : _linguisticEngine;
    }

    private OcrRegionRead ReadNameWithMultilineConsensus(
        global::Tesseract.Pix image,
        Luminance8Frame frame)
    {
        using var linguisticPage = _linguisticEngine.Process(
            image,
            global::Tesseract.PageSegMode.SingleBlock);
        var linguisticText = linguisticPage.GetText();
        var confidence = linguisticPage.GetMeanConfidence();

        if (!HasMultipleTextLines(linguisticText))
        {
            return new OcrRegionRead(
                linguisticText,
                confidence,
                !string.IsNullOrWhiteSpace(linguisticText) &&
                TouchesBoundary(linguisticPage, frame));
        }

        using var verificationPage = _fastEngine.Process(
            image,
            global::Tesseract.PageSegMode.SingleBlock);
        var verificationText = verificationPage.GetText();
        confidence = Math.Min(
            confidence,
            verificationPage.GetMeanConfidence());

        if (!string.Equals(
                NormalizeWhitespace(linguisticText),
                NormalizeWhitespace(verificationText),
                StringComparison.Ordinal))
        {
            return new OcrRegionRead(null, confidence);
        }

        return new OcrRegionRead(
            linguisticText,
            confidence,
            !string.IsNullOrWhiteSpace(linguisticText) &&
            (TouchesBoundary(linguisticPage, frame) ||
             TouchesBoundary(verificationPage, frame)));
    }

    private static bool HasMultipleTextLines(string value)
    {
        return value
            .Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.RemoveEmptyEntries)
            .Length > 1;
    }

    private static string NormalizeWhitespace(string value)
    {
        return string.Join(
            ' ',
            value.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries));
    }

    private OcrRegionRead ReadDestinationWithCompactCodeConsensus(
        global::Tesseract.Pix image,
        Luminance8Frame frame)
    {
        using var linguisticPage = _linguisticEngine.Process(
            image,
            global::Tesseract.PageSegMode.SingleLine);
        var linguisticText = linguisticPage.GetText();
        var confidence = linguisticPage.GetMeanConfidence();

        if (!HasAdjacentLetterAndDigit(linguisticText))
        {
            return new OcrRegionRead(
                linguisticText,
                confidence,
                !string.IsNullOrWhiteSpace(linguisticText) &&
                TouchesBoundary(linguisticPage, frame));
        }

        using var verificationPage = _fastEngine.Process(
            image,
            global::Tesseract.PageSegMode.SingleLine);
        var verificationText = verificationPage.GetText();
        confidence = Math.Min(
            confidence,
            verificationPage.GetMeanConfidence());

        if (!string.Equals(
                NormalizeWhitespace(linguisticText),
                NormalizeWhitespace(verificationText),
                StringComparison.Ordinal))
        {
            return new OcrRegionRead(null, confidence);
        }

        return new OcrRegionRead(
            linguisticText,
            confidence,
            !string.IsNullOrWhiteSpace(linguisticText) &&
            (TouchesBoundary(linguisticPage, frame) ||
             TouchesBoundary(verificationPage, frame)));
    }

    private static bool HasAdjacentLetterAndDigit(string value)
    {
        for (var index = 1; index < value.Length; index++)
        {
            var previous = value[index - 1];
            var current = value[index];

            if ((char.IsLetter(previous) && char.IsDigit(current)) ||
                (char.IsDigit(previous) && char.IsLetter(current)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasNoTrustworthyContrast(Luminance8Frame frame)
    {
        var pixels = frame.PixelData.Span;
        var minimum = pixels[0];
        var maximum = pixels[0];

        for (var index = 1; index < pixels.Length; index++)
        {
            minimum = Math.Min(minimum, pixels[index]);
            maximum = Math.Max(maximum, pixels[index]);

            if (maximum - minimum > MaximumBlankLuminanceRange)
            {
                return false;
            }
        }

        return true;
    }

    private static bool TouchesBoundary(
        global::Tesseract.Page page,
        Luminance8Frame frame)
    {
        var rightBoundary = frame.Width - BoundarySafetyMarginPixels;
        var bottomBoundary = frame.Height - BoundarySafetyMarginPixels;

        foreach (var bounds in page.GetSegmentedRegions(
                     global::Tesseract.PageIteratorLevel.Symbol))
        {
            if (bounds.Left <= BoundarySafetyMarginPixels ||
                bounds.Top <= BoundarySafetyMarginPixels ||
                bounds.Right >= rightBoundary ||
                bounds.Bottom >= bottomBoundary)
            {
                return true;
            }
        }

        return false;
    }

    private static global::Tesseract.PageSegMode PageSegmentationFor(
        OcrFieldKind field)
    {
        return field switch
        {
            OcrFieldKind.Name => global::Tesseract.PageSegMode.SingleBlock,
            OcrFieldKind.Password => global::Tesseract.PageSegMode.SingleLine,
            OcrFieldKind.Sector => global::Tesseract.PageSegMode.SingleLine,
            OcrFieldKind.Destination => global::Tesseract.PageSegMode.SingleLine,
            _ => throw new ArgumentOutOfRangeException(
                nameof(field),
                field,
                "Unsupported OCR field."),
        };
    }
}
