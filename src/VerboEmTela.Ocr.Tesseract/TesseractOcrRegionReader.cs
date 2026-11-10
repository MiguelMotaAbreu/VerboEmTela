using VerboEmTela.Core.Imaging;
using VerboEmTela.Core.Ocr;

namespace VerboEmTela.Ocr.Tesseract;

public sealed class TesseractOcrRegionReader : IOcrRegionReader, IDisposable
{
    private readonly global::Tesseract.TesseractEngine _engine;
    private readonly object _sync = new();
    private bool _disposed;

    public TesseractOcrRegionReader(string tessdataPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tessdataPath);

        if (!string.IsNullOrEmpty(
            Environment.GetEnvironmentVariable("TESSDATA_PREFIX")))
        {
            throw new InvalidOperationException(
                "TESSDATA_PREFIX must be unset so the packaged OCR model cannot be overridden.");
        }

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

        _engine = new global::Tesseract.TesseractEngine(
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
            using var page = _engine.Process(
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

            _engine.Dispose();
            _disposed = true;
        }
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
