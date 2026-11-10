using VerboEmTela.Core.Imaging;
using VerboEmTela.Core.Ocr;

namespace VerboEmTela.Ocr.Tesseract;

public sealed class TesseractOcrRegionReader : IOcrRegionReader, IDisposable
{
    private readonly global::Tesseract.TesseractEngine _passwordEngine;
    private readonly global::Tesseract.TesseractEngine _linguisticEngine;
    private readonly object _sync = new();
    private bool _disposed;

    public TesseractOcrRegionReader(
        string passwordTessdataPath,
        string linguisticTessdataPath)
    {
        if (!string.IsNullOrEmpty(
            Environment.GetEnvironmentVariable("TESSDATA_PREFIX")))
        {
            throw new InvalidOperationException(
                "TESSDATA_PREFIX must be unset so the packaged OCR model cannot be overridden.");
        }

        _passwordEngine = CreateEngine(passwordTessdataPath);

        try
        {
            _linguisticEngine = CreateEngine(linguisticTessdataPath);
        }
        catch
        {
            _passwordEngine.Dispose();
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

            using var image = global::Tesseract.Pix.LoadFromMemory(
                GrayscaleBmpEncoder.Encode(frame));
            using var page = EngineFor(field).Process(
                image,
                PageSegmentationFor(field));

            return new OcrRegionRead(
                page.GetText(),
                page.GetMeanConfidence());
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
                _passwordEngine.Dispose();
            }

            _disposed = true;
        }
    }

    private global::Tesseract.TesseractEngine EngineFor(OcrFieldKind field)
    {
        return field is OcrFieldKind.Password
            ? _passwordEngine
            : _linguisticEngine;
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
