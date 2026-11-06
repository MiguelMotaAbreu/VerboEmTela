using System.Text.Json;
using System.Text.Json.Serialization;

namespace VerboEmTela.Windows.Configuration;

public sealed class MonitoringConfigurationLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public MonitoringConfiguration LoadFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ConfigurationException(
                "O caminho do arquivo de configuração não foi informado.");
        }

        try
        {
            var fullPath = Path.GetFullPath(path.Trim());
            var json = File.ReadAllText(fullPath);
            return Parse(json);
        }
        catch (ConfigurationException)
        {
            throw;
        }
        catch (Exception exception)
            when (exception is ArgumentException or
                  IOException or
                  NotSupportedException or
                  UnauthorizedAccessException)
        {
            throw new ConfigurationException(
                "Não foi possível ler o arquivo de configuração informado.",
                exception);
        }
    }

    internal MonitoringConfiguration Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ConfigurationException(
                "O arquivo de configuração está vazio.");
        }

        MonitoringConfiguration? configuration;

        try
        {
            configuration = JsonSerializer.Deserialize<MonitoringConfiguration>(
                json,
                SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new ConfigurationException(
                "O arquivo de configuração contém JSON inválido ou campos desconhecidos.",
                exception);
        }

        if (configuration is null)
        {
            throw new ConfigurationException(
                "O arquivo de configuração não contém um objeto válido.");
        }

        configuration.Validate();
        return configuration;
    }
}
