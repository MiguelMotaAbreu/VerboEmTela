using System.Text;
using VerboEmTela.Core.Detection;
using VerboEmTela.Core.Layout;
using VerboEmTela.Windows.Application;
using VerboEmTela.Windows.Capture;

Console.OutputEncoding = Encoding.UTF8;

using var cancellation = new CancellationTokenSource();
ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

Console.CancelKeyPress += cancelHandler;

try
{
    return await RunAsync(cancellation.Token);
}
finally
{
    Console.CancelKeyPress -= cancelHandler;
}

static async Task<int> RunAsync(CancellationToken cancellationToken)
{
    try
    {
        using var instanceLease = SingleInstanceLease.TryAcquire(
            @"Local\VerboEmTela.SingleInstance");

        if (instanceLease is null)
        {
            Console.Error.WriteLine(
                "O VerboEmTela já está em execução nesta sessão do Windows.");
            return 5;
        }

        Console.WriteLine("VerboEmTela — diagnóstico visual sem OCR e sem voz");
        Console.WriteLine("Procurando um único painel válido nos monitores disponíveis...");

        var layoutRecognizer = new PanelLayoutRecognizer();
        var pixelCapture = new GdiScreenPixelCapture();
        var selector = new PanelMonitorSelector(
            new Win32DisplayMonitorProvider(),
            pixelCapture,
            layoutRecognizer);
        var selection = selector.Select();

        if (selection.Status is not PanelMonitorSelectionStatus.Selected)
        {
            return ReportSelectionFailure(selection);
        }

        var monitor = selection.Monitor!;
        Console.WriteLine(
            $"Monitor selecionado: {monitor.DeviceName} " +
            $"({monitor.Bounds.Width}x{monitor.Bounds.Height} em " +
            $"{monitor.Bounds.Left},{monitor.Bounds.Top}).");
        Console.WriteLine("Monitorando a 5 FPS. Pressione Ctrl+C para encerrar.");

        var frameSource = new ScreenFrameSource(
            pixelCapture,
            new ScreenFrameSourceOptions
            {
                Region = monitor.Bounds,
                FramesPerSecond = 5,
            });
        var monitoringRunner = new LayoutMonitoringRunner(layoutRecognizer);

        await foreach (var state in monitoringRunner
            .RunAsync(frameSource, cancellationToken)
            .ConfigureAwait(false))
        {
            Console.WriteLine(FormatState(state));
        }

        return 0;
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        Console.WriteLine("Monitoramento encerrado.");
        return 0;
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"Falha técnica: {exception.Message}");
        return 10;
    }
}

static int ReportSelectionFailure(PanelMonitorSelectionResult selection)
{
    var (message, exitCode) = selection.Status switch
    {
        PanelMonitorSelectionStatus.NoMonitorsAvailable =>
            ("O Windows não informou monitores disponíveis.", 2),
        PanelMonitorSelectionStatus.NoPanelFound =>
            ($"Nenhum painel válido foi encontrado nos " +
             $"{selection.ScannedMonitorCount} monitores examinados.", 3),
        PanelMonitorSelectionStatus.MultiplePanelsFound =>
            ($"Foram encontrados {selection.MatchingMonitorCount} painéis válidos; " +
             "a seleção é ambígua.", 4),
        _ => ("A seleção do monitor retornou um estado inesperado.", 10),
    };

    Console.Error.WriteLine(message);
    return exitCode;
}

static string FormatState(LayoutMonitoringEvent state)
{
    var elapsed = state.ObservedAt.ToString(@"hh\:mm\:ss\.fff");
    var description = state.Mode switch
    {
        PanelMode.Dashboard => "painel normal",
        PanelMode.FullScreenCall => "chamada em tela cheia",
        PanelMode.Invalid => $"painel inválido ({Describe(state.RejectionReason)})",
        _ => "estado desconhecido",
    };

    return $"[{elapsed}] {description}";
}

static string Describe(LayoutRejectionReason reason)
{
    return reason switch
    {
        LayoutRejectionReason.UnexpectedDimensions => "dimensões inesperadas",
        LayoutRejectionReason.InsufficientLightBackground => "fundo visual insuficiente",
        LayoutRejectionReason.UnexpectedGreenBandCount => "faixas do painel não reconhecidas",
        LayoutRejectionReason.FirstBandOutsideExpectedRegion => "posição do painel inesperada",
        _ => "motivo não identificado",
    };
}
