using VerboEmTela.Core.Detection;
using VerboEmTela.Core.Imaging;

namespace VerboEmTela.Core.Layout;

public sealed class PanelLayoutRecognizer : IPanelLayoutRecognizer
{
    private const byte MinimumGreen = 45;
    private const byte MaximumGreen = 155;
    private const byte MaximumRed = 125;
    private const byte MaximumBlue = 125;
    private const int MinimumGreenLead = 15;
    private const byte MinimumLightChannel = 200;

    private readonly PanelLayoutRecognizerOptions _options;

    public PanelLayoutRecognizer(PanelLayoutRecognizerOptions? options = null)
    {
        _options = options ?? new PanelLayoutRecognizerOptions();
        _options.Validate();
    }

    public LayoutRecognition Recognize(Rgb24Frame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (frame.Width != _options.RequiredWidth || frame.Height != _options.RequiredHeight)
        {
            return Invalid(LayoutRejectionReason.UnexpectedDimensions);
        }

        var scan = Scan(frame);
        if (scan.LightPixelRatio < _options.MinimumLightPixelRatio)
        {
            return Invalid(LayoutRejectionReason.InsufficientLightBackground, scan.Bands);
        }

        if (scan.Bands.Count is not 1 && scan.Bands.Count != _options.DashboardBandCount)
        {
            return Invalid(LayoutRejectionReason.UnexpectedGreenBandCount, scan.Bands);
        }

        var firstBandTopRatio = scan.Bands[0].Top / (double)frame.Height;
        if (firstBandTopRatio < _options.MinimumFirstBandTopRatio ||
            firstBandTopRatio > _options.MaximumFirstBandTopRatio)
        {
            return Invalid(LayoutRejectionReason.FirstBandOutsideExpectedRegion, scan.Bands);
        }

        var mode = scan.Bands.Count == 1
            ? PanelMode.FullScreenCall
            : PanelMode.Dashboard;

        return new LayoutRecognition(mode, scan.Bands, LayoutRejectionReason.None);
    }

    private ScanResult Scan(Rgb24Frame frame)
    {
        var pixels = frame.Pixels;
        var rowCoverage = new double[frame.Height];
        long lightPixels = 0;

        for (var y = 0; y < frame.Height; y++)
        {
            var greenPixels = 0;
            var rowStart = y * frame.Width * 3;

            for (var x = 0; x < frame.Width; x++)
            {
                var pixelStart = rowStart + (x * 3);
                var red = pixels[pixelStart];
                var green = pixels[pixelStart + 1];
                var blue = pixels[pixelStart + 2];

                if (IsPanelGreen(red, green, blue))
                {
                    greenPixels++;
                }

                if (red >= MinimumLightChannel &&
                    green >= MinimumLightChannel &&
                    blue >= MinimumLightChannel)
                {
                    lightPixels++;
                }
            }

            rowCoverage[y] = greenPixels / (double)frame.Width;
        }

        var bands = FindBands(rowCoverage, frame.Height);
        var pixelCount = (long)frame.Width * frame.Height;
        return new ScanResult(bands, lightPixels / (double)pixelCount);
    }

    private IReadOnlyList<HorizontalBand> FindBands(double[] rowCoverage, int frameHeight)
    {
        var minimumBandHeight = Math.Max(
            1,
            (int)Math.Ceiling(frameHeight * _options.MinimumBandHeightRatio));
        var maximumGap = (int)Math.Floor(frameHeight * _options.MaximumGapInsideBandRatio);
        var bands = new List<HorizontalBand>();

        var start = -1;
        var lastMatchingRow = -1;
        var coverageSum = 0d;
        var matchingRows = 0;

        for (var y = 0; y < rowCoverage.Length; y++)
        {
            if (rowCoverage[y] < _options.MinimumGreenRowCoverage)
            {
                continue;
            }

            if (start >= 0 && y - lastMatchingRow - 1 > maximumGap)
            {
                AddBandIfTallEnough(
                    bands,
                    start,
                    lastMatchingRow,
                    coverageSum,
                    matchingRows,
                    minimumBandHeight);

                start = -1;
                coverageSum = 0;
                matchingRows = 0;
            }

            if (start < 0)
            {
                start = y;
            }

            lastMatchingRow = y;
            coverageSum += rowCoverage[y];
            matchingRows++;
        }

        if (start >= 0)
        {
            AddBandIfTallEnough(
                bands,
                start,
                lastMatchingRow,
                coverageSum,
                matchingRows,
                minimumBandHeight);
        }

        return bands;
    }

    private static void AddBandIfTallEnough(
        ICollection<HorizontalBand> bands,
        int start,
        int lastMatchingRow,
        double coverageSum,
        int matchingRows,
        int minimumBandHeight)
    {
        var height = lastMatchingRow - start + 1;
        if (height < minimumBandHeight)
        {
            return;
        }

        bands.Add(new HorizontalBand(
            start,
            lastMatchingRow + 1,
            coverageSum / matchingRows));
    }

    private static bool IsPanelGreen(byte red, byte green, byte blue)
    {
        return red <= MaximumRed &&
               green >= MinimumGreen &&
               green <= MaximumGreen &&
               blue <= MaximumBlue &&
               green - red >= MinimumGreenLead &&
               green - blue >= MinimumGreenLead;
    }

    private static LayoutRecognition Invalid(
        LayoutRejectionReason reason,
        IReadOnlyList<HorizontalBand>? bands = null)
    {
        return new LayoutRecognition(
            PanelMode.Invalid,
            bands ?? Array.Empty<HorizontalBand>(),
            reason);
    }

    private sealed record ScanResult(
        IReadOnlyList<HorizontalBand> Bands,
        double LightPixelRatio);
}
