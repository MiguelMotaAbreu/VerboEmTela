using VerboEmTela.Core.Ocr;

namespace VerboEmTela.Core.Announcements;

public sealed class DestinationPronunciationMap
{
    private readonly Dictionary<string, string> _pronunciations = new(StringComparer.OrdinalIgnoreCase);

    public DestinationPronunciationMap(IEnumerable<KeyValuePair<string, string>> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        foreach (var (destination, pronunciation) in entries)
        {
            var key = Normalize(destination);
            var spokenText = Normalize(pronunciation);
            if (key is null || spokenText is null)
            {
                throw new ArgumentException("Destination mappings require valid keys and wording.", nameof(entries));
            }

            if (!_pronunciations.TryAdd(key, spokenText))
            {
                throw new ArgumentException("Destination mappings contain a duplicate key.", nameof(entries));
            }
        }
    }

    internal string? Find(string destination)
    {
        var key = Normalize(destination);
        return key is not null && _pronunciations.TryGetValue(key, out var pronunciation)
            ? pronunciation
            : null;
    }

    private static string? Normalize(string text) =>
        OcrTextNormalizer.Normalize(OcrFieldKind.Destination, new OcrRegionRead(text)).Text;
}
