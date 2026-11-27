using SherpaOnnx;
using VerboEmTela.Core.Speech;

namespace VerboEmTela.Speech.SherpaOnnx;

public sealed class FaberSpeechEngine : ISpeechEngine, IDisposable
{
    private readonly object _sync = new();
    private readonly OfflineTts _engine;
    private bool _disposed;

    public FaberSpeechEngine(string modelDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelDirectory);

        var directory = Path.GetFullPath(modelDirectory);
        var dataDirectory = Path.Combine(directory, "espeak-ng-data");
        var config = new OfflineTtsConfig();
        config.Model.Vits.Model = RequireFile(directory, "pt_BR-faber-medium.onnx");
        config.Model.Vits.Tokens = RequireFile(directory, "tokens.txt");
        _ = RequireFile(dataDirectory, "phontab");
        config.Model.Vits.DataDir = dataDirectory;
        config.Model.Provider = "cpu";
        config.Model.NumThreads = 1;

        _engine = new OfflineTts(config);
    }

    public SynthesizedSpeech Synthesize(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (text.Contains('\0'))
        {
            throw new ArgumentException("Speech input cannot contain a null character.", nameof(text));
        }

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var audio = _engine.Generate(text, 1f, 0);
            try
            {
                if (audio.Handle == IntPtr.Zero || audio.NumSamples <= 0 || audio.SampleRate <= 0)
                {
                    throw new InvalidOperationException("The local Faber voice did not produce audio.");
                }

                return new SynthesizedSpeech(audio.Samples, audio.SampleRate);
            }
            finally
            {
                audio.Dispose();
            }
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

    private static string RequireFile(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);
        return File.Exists(path)
            ? path
            : throw new FileNotFoundException("A required local voice file was not found.", path);
    }
}
