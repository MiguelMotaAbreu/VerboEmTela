namespace VerboEmTela.Windows.Configuration;

public sealed record MonitoringConfiguration
{
    public const int CurrentSchemaVersion = 1;
    public const int SupportedWidth = 1920;
    public const int SupportedHeight = 1080;
    public const double DefaultFramesPerSecond = 5;
    public const double MaximumFramesPerSecond = 10;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public int RequiredWidth { get; init; } = SupportedWidth;

    public int RequiredHeight { get; init; } = SupportedHeight;

    public double FramesPerSecond { get; init; } = DefaultFramesPerSecond;

    public static MonitoringConfiguration SafeDefault => new();

    internal void Validate()
    {
        if (SchemaVersion != CurrentSchemaVersion)
        {
            throw new ConfigurationException(
                $"Versão de configuração incompatível: {SchemaVersion}. " +
                $"A versão suportada é {CurrentSchemaVersion}.");
        }

        if (RequiredWidth != SupportedWidth || RequiredHeight != SupportedHeight)
        {
            throw new ConfigurationException(
                $"A resolução {RequiredWidth}x{RequiredHeight} ainda não foi validada. " +
                $"Esta versão aceita somente {SupportedWidth}x{SupportedHeight}.");
        }

        if (double.IsNaN(FramesPerSecond) ||
            double.IsInfinity(FramesPerSecond) ||
            FramesPerSecond < DefaultFramesPerSecond ||
            FramesPerSecond > MaximumFramesPerSecond)
        {
            throw new ConfigurationException(
                $"A frequência deve estar entre {DefaultFramesPerSecond} e " +
                $"{MaximumFramesPerSecond} FPS.");
        }
    }
}
