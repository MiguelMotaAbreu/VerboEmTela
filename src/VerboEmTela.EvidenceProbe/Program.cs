using System.Text;
using VerboEmTela.Core.Detection;
using VerboEmTela.Core.Layout;
using VerboEmTela.Windows.Evidence;

Console.OutputEncoding = Encoding.UTF8;

if (args.Length is < 1 or > 2)
{
    Console.Error.WriteLine(
        "Uso: VerboEmTela.EvidenceProbe <video.mp4> [intervalo-ms]");
    return 2;
}

var intervalMilliseconds = 500;
if (args.Length == 2 &&
    (!int.TryParse(args[1], out intervalMilliseconds) ||
     intervalMilliseconds is < 100 or > 5_000))
{
    Console.Error.WriteLine("O intervalo deve estar entre 100 e 5000 ms.");
    return 2;
}

using var cancellation = new CancellationTokenSource();
ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};
Console.CancelKeyPress += cancelHandler;

try
{
    var interval = TimeSpan.FromMilliseconds(intervalMilliseconds);
    var recognizer = new PanelLayoutRecognizer();
    var observations = new List<EvidenceObservation>();

    await new WpfVideoFrameReader().ReadAtIntervalAsync(
        args[0],
        interval,
        frame =>
        {
            var recognition = recognizer.Recognize(frame.Frame);
            observations.Add(new EvidenceObservation(
                frame.ObservedAt,
                recognition.Mode,
                recognition.RejectionReason,
                recognition.GreenBands));
        },
        cancellation.Token);

    Console.WriteLine(
        $"{observations.Count} amostras processadas em memória; " +
        "nenhum quadro ou texto foi gravado.");

    foreach (var run in GroupRuns(observations))
    {
        Console.WriteLine(
            $"{Format(run.Start)}–{Format(run.End)}  " +
            $"{Describe(run.Mode, run.RejectionReason)}  " +
            $"({run.SampleCount} amostras)");
    }

    WriteBandSummary(observations);

    return 0;
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    Console.WriteLine("Leitura cancelada.");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Falha ao analisar a evidência: {exception.Message}");
    return 10;
}
finally
{
    Console.CancelKeyPress -= cancelHandler;
}

static IEnumerable<EvidenceRun> GroupRuns(
    IReadOnlyList<EvidenceObservation> observations)
{
    if (observations.Count == 0)
    {
        yield break;
    }

    var first = observations[0];
    var start = first.ObservedAt;
    var previous = first;
    var count = 1;

    for (var index = 1; index < observations.Count; index++)
    {
        var current = observations[index];
        if (current.Mode == previous.Mode &&
            current.RejectionReason == previous.RejectionReason)
        {
            previous = current;
            count++;
            continue;
        }

        yield return new EvidenceRun(
            start,
            previous.ObservedAt,
            previous.Mode,
            previous.RejectionReason,
            count);
        start = current.ObservedAt;
        previous = current;
        count = 1;
    }

    yield return new EvidenceRun(
        start,
        previous.ObservedAt,
        previous.Mode,
        previous.RejectionReason,
        count);
}

static string Format(TimeSpan value) => value.ToString(@"mm\:ss\.fff");

static string Describe(
    PanelMode mode,
    LayoutRejectionReason rejectionReason)
{
    return mode switch
    {
        PanelMode.Dashboard => "painel normal",
        PanelMode.FullScreenCall => "chamada em tela cheia",
        PanelMode.Invalid => $"inválido: {rejectionReason}",
        _ => "estado desconhecido",
    };
}

static void WriteBandSummary(
    IReadOnlyList<EvidenceObservation> observations)
{
    Console.WriteLine("Resumo estrutural das faixas verdes:");

    foreach (var group in observations
        .GroupBy(observation => observation.GreenBands.Count)
        .OrderBy(group => group.Key))
    {
        Console.WriteLine(
            $"- {group.Key} faixa(s): {group.Count()} amostra(s)");

        for (var bandIndex = 0; bandIndex < group.Key; bandIndex++)
        {
            var bands = group
                .Select(observation => observation.GreenBands[bandIndex])
                .ToArray();
            Console.WriteLine(
                $"  faixa {bandIndex + 1}: " +
                $"topo {bands.Min(band => band.Top)}–{bands.Max(band => band.Top)}, " +
                $"altura {bands.Min(band => band.Height)}–{bands.Max(band => band.Height)}, " +
                $"cobertura {bands.Min(band => band.AverageGreenCoverage):P1}–" +
                $"{bands.Max(band => band.AverageGreenCoverage):P1}");
        }
    }
}

sealed record EvidenceObservation(
    TimeSpan ObservedAt,
    PanelMode Mode,
    LayoutRejectionReason RejectionReason,
    IReadOnlyList<HorizontalBand> GreenBands);

sealed record EvidenceRun(
    TimeSpan Start,
    TimeSpan End,
    PanelMode Mode,
    LayoutRejectionReason RejectionReason,
    int SampleCount);
