using System.Text;
using VerboEmTela.Core.Application;
using VerboEmTela.Core.Detection;
using VerboEmTela.Core.Frames;
using VerboEmTela.Core.Layout;
using VerboEmTela.Windows.Application;
using VerboEmTela.Windows.Capture;
using VerboEmTela.Windows.Configuration;

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
    return await RunAsync(args, cancellation);
}
finally
{
    Console.CancelKeyPress -= cancelHandler;
}

static async Task<int> RunAsync(
    string[] commandLineArguments,
    CancellationTokenSource cancellation)
{
    var cancellationToken = cancellation.Token;
    var stateMachine = new ApplicationStateMachine(
        new DiagnosticTransientContentCleaner());

    try
    {
        using var instanceLease = SingleInstanceLease.TryAcquire(
            @"Local\VerboEmTela.SingleInstance");

        if (instanceLease is null)
        {
            stateMachine.Stop();
            Console.Error.WriteLine(
                "O VerboEmTela já está em execução nesta sessão do Windows.");
            return 5;
        }

        Console.WriteLine("VerboEmTela — diagnóstico visual sem OCR e sem voz");
        WriteApplicationStatus(stateMachine.Current);
        var configuration = LoadConfiguration(commandLineArguments);
        Console.WriteLine(
            $"Configuração válida: {configuration.RequiredWidth}x" +
            $"{configuration.RequiredHeight}, {configuration.FramesPerSecond:g} FPS.");
        Console.WriteLine("Procurando um único painel válido nos monitores disponíveis...");

        var layoutRecognizer = new PanelLayoutRecognizer(
            new PanelLayoutRecognizerOptions
            {
                RequiredWidth = configuration.RequiredWidth,
                RequiredHeight = configuration.RequiredHeight,
            });
        var pixelCapture = new GdiScreenPixelCapture();
        var selector = new PanelMonitorSelector(
            new Win32DisplayMonitorProvider(),
            pixelCapture,
            layoutRecognizer);
        var selection = selector.Select();

        if (selection.Status is not PanelMonitorSelectionStatus.Selected)
        {
            WriteApplicationStatus(stateMachine.Fail(
                "Não foi possível selecionar um único painel válido."));
            return ReportSelectionFailure(selection);
        }

        var monitor = selection.Monitor!;
        Console.WriteLine(
            $"Monitor selecionado: {monitor.DeviceName} " +
            $"({monitor.Bounds.Width}x{monitor.Bounds.Height} em " +
            $"{monitor.Bounds.Left},{monitor.Bounds.Top}).");
        Console.WriteLine(
            $"Monitorando a {configuration.FramesPerSecond:g} FPS. " +
            "Pressione Ctrl+C para encerrar.");
        Console.WriteLine("Comandos: P = pausar, R = retomar, Q = encerrar.");
        WriteApplicationStatus(stateMachine.CompleteStartup());

        var captureGate = new FrameCaptureGate();
        var frameSource = new ScreenFrameSource(
            pixelCapture,
            new ScreenFrameSourceOptions
            {
                Region = monitor.Bounds,
                FramesPerSecond = configuration.FramesPerSecond,
            },
            captureGate: captureGate);
        var monitoringRunner = new LayoutMonitoringRunner(layoutRecognizer);
        var commandTask = ReadCommandsAsync(
            captureGate,
            stateMachine,
            cancellation);

        try
        {
            await foreach (var state in monitoringRunner
                .RunAsync(frameSource, cancellationToken)
                .ConfigureAwait(false))
            {
                if (stateMachine.Current.State is ApplicationState.Paused)
                {
                    continue;
                }

                UpdateApplicationState(stateMachine, state);
                Console.WriteLine(FormatState(state));
            }
        }
        finally
        {
            cancellation.Cancel();

            try
            {
                await commandTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
            }
        }

        WriteApplicationStatus(stateMachine.Stop());
        return 0;
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        if (stateMachine.Current.State is not ApplicationState.Stopped)
        {
            WriteApplicationStatus(stateMachine.Stop());
        }

        Console.WriteLine("Monitoramento encerrado.");
        return 0;
    }
    catch (ConfigurationException exception)
    {
        WriteApplicationStatus(stateMachine.Fail(exception.Message));
        Console.Error.WriteLine($"Configuração inválida: {exception.Message}");
        return 6;
    }
    catch (Exception exception)
    {
        if (stateMachine.Current.State is not ApplicationState.Stopped)
        {
            WriteApplicationStatus(stateMachine.Fail(exception.Message));
        }

        Console.Error.WriteLine($"Falha técnica: {exception.Message}");
        return 10;
    }
}

static async Task ReadCommandsAsync(
    FrameCaptureGate captureGate,
    ApplicationStateMachine stateMachine,
    CancellationTokenSource cancellation)
{
    while (!cancellation.IsCancellationRequested)
    {
        var command = await Console.In
            .ReadLineAsync(cancellation.Token)
            .ConfigureAwait(false);

        if (command is null)
        {
            return;
        }

        switch (command.Trim().ToUpperInvariant())
        {
            case "P":
                if (stateMachine.Current.State is ApplicationState.Paused)
                {
                    Console.WriteLine("O monitoramento já está pausado.");
                    break;
                }

                WriteApplicationStatus(stateMachine.Pause());
                captureGate.Pause();
                break;

            case "R":
                if (stateMachine.Current.State is not ApplicationState.Paused)
                {
                    Console.WriteLine("O monitoramento não está pausado.");
                    break;
                }

                WriteApplicationStatus(stateMachine.Resume());
                captureGate.Resume();
                break;

            case "Q":
                cancellation.Cancel();
                return;

            case "":
                break;

            default:
                Console.WriteLine("Comando inválido. Use P, R ou Q.");
                break;
        }
    }
}

static void UpdateApplicationState(
    ApplicationStateMachine stateMachine,
    LayoutMonitoringEvent layout)
{
    if (layout.Mode is PanelMode.Dashboard &&
        stateMachine.Current.State is ApplicationState.Arming)
    {
        WriteApplicationStatus(stateMachine.ConfirmPanelReady());
    }
    else if (layout.Mode is PanelMode.Invalid &&
             stateMachine.Current.State is
                 ApplicationState.Monitoring or ApplicationState.Speaking)
    {
        WriteApplicationStatus(stateMachine.ReportPanelLost());
    }
}

static void WriteApplicationStatus(ApplicationStatus status)
{
    var description = status.State switch
    {
        ApplicationState.Starting => "Iniciando",
        ApplicationState.Arming => "Armando",
        ApplicationState.Monitoring => "Monitorando",
        ApplicationState.Speaking => "Falando",
        ApplicationState.Paused => "Pausado",
        ApplicationState.Error => $"Erro — {status.ErrorReason}",
        ApplicationState.Stopped => "Encerrado",
        _ => "Desconhecido",
    };

    Console.WriteLine($"Estado da aplicação: {description}");
}

static MonitoringConfiguration LoadConfiguration(string[] commandLineArguments)
{
    if (commandLineArguments.Length == 0)
    {
        return MonitoringConfiguration.SafeDefault;
    }

    if (commandLineArguments.Length == 2 &&
        string.Equals(
            commandLineArguments[0],
            "--config",
            StringComparison.OrdinalIgnoreCase))
    {
        return new MonitoringConfigurationLoader().LoadFile(
            commandLineArguments[1]);
    }

    throw new ConfigurationException(
        "Uso esperado: VerboEmTela.Diagnostic [--config <arquivo.json>].");
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

sealed class DiagnosticTransientContentCleaner : ITransientContentCleaner
{
    public void Clear()
    {
    }
}
