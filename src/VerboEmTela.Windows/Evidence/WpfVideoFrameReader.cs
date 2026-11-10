using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VerboEmTela.Core.Frames;
using VerboEmTela.Core.Imaging;
using VerboEmTela.Windows.Capture;

namespace VerboEmTela.Windows.Evidence;

public sealed class WpfVideoFrameReader
{
    private static readonly TimeSpan SeekSettleTime = TimeSpan.FromMilliseconds(100);

    public Task ReadAtIntervalAsync(
        string filePath,
        TimeSpan interval,
        Action<CapturedFrame> processFrame,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(processFrame);

        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval));
        }

        var fullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Video evidence file was not found.", fullPath);
        }

        var completion = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() => RunOnStaThread(
            fullPath,
            interval,
            processFrame,
            cancellationToken,
            completion))
        {
            IsBackground = true,
            Name = "VerboEmTela.VideoEvidence",
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return completion.Task;
    }

    private static void RunOnStaThread(
        string fullPath,
        TimeSpan interval,
        Action<CapturedFrame> processFrame,
        CancellationToken cancellationToken,
        TaskCompletionSource completion)
    {
        var dispatcher = Dispatcher.CurrentDispatcher;

        _ = dispatcher.InvokeAsync(async () =>
        {
            try
            {
                await ReadCoreAsync(
                    fullPath,
                    interval,
                    processFrame,
                    cancellationToken);
                completion.TrySetResult();
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                completion.TrySetCanceled(cancellationToken);
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
            finally
            {
                dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
            }
        });

        Dispatcher.Run();
    }

    private static async Task ReadCoreAsync(
        string fullPath,
        TimeSpan interval,
        Action<CapturedFrame> processFrame,
        CancellationToken cancellationToken)
    {
        var player = new MediaPlayer
        {
            ScrubbingEnabled = true,
            Volume = 0,
        };
        var opened = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        void OnOpened(object? sender, EventArgs eventArgs) =>
            opened.TrySetResult(true);
        void OnFailed(object? sender, ExceptionEventArgs eventArgs) =>
            opened.TrySetException(
                eventArgs.ErrorException ??
                new InvalidOperationException("Windows could not decode the video evidence."));

        player.MediaOpened += OnOpened;
        player.MediaFailed += OnFailed;

        try
        {
            player.Open(new Uri(fullPath, UriKind.Absolute));
            await opened.Task.WaitAsync(cancellationToken);

            if (!player.NaturalDuration.HasTimeSpan)
            {
                throw new InvalidOperationException(
                    "Windows did not expose a finite duration for the video evidence.");
            }

            if (player.NaturalVideoWidth <= 0 || player.NaturalVideoHeight <= 0)
            {
                throw new InvalidOperationException(
                    "Windows did not expose valid video dimensions.");
            }

            player.Pause();
            var duration = player.NaturalDuration.TimeSpan;
            for (var position = TimeSpan.Zero;
                 position < duration;
                 position += interval)
            {
                cancellationToken.ThrowIfCancellationRequested();
                player.Position = position;
                await Task.Delay(SeekSettleTime, cancellationToken);
                processFrame(new CapturedFrame(Capture(player), position));
            }
        }
        finally
        {
            player.MediaOpened -= OnOpened;
            player.MediaFailed -= OnFailed;
            player.Close();
        }
    }

    private static Rgb24Frame Capture(MediaPlayer player)
    {
        var width = player.NaturalVideoWidth;
        var height = player.NaturalVideoHeight;
        var visual = new DrawingVisual();

        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawVideo(player, new Rect(0, 0, width, height));
        }

        var bitmap = new RenderTargetBitmap(
            width,
            height,
            96,
            96,
            PixelFormats.Pbgra32);
        bitmap.Render(visual);

        var stride = checked(width * 4);
        var pixels = new byte[checked(stride * height)];
        bitmap.CopyPixels(pixels, stride, 0);

        return new Rgb24Frame(
            width,
            height,
            PixelBufferConverter.Bgra32ToRgb24(
                pixels,
                width,
                height,
                stride));
    }
}
