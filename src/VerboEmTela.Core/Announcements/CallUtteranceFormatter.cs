using System.Text;
using VerboEmTela.Core.Detection;

namespace VerboEmTela.Core.Announcements;

public static class CallUtteranceFormatter
{
    private static readonly string[] LetterNames =
    [
        "a", "bê", "cê", "dê", "é", "efe", "gê", "agá", "i", "jota",
        "cá", "ele", "eme", "ene", "ó", "pê", "quê", "erre", "esse", "tê",
        "u", "vê", "dáblio", "xis", "ípsilon", "zê",
    ];

    private static readonly string[] DigitNames =
    [
        "zero", "um", "dois", "três", "quatro",
        "cinco", "seis", "sete", "oito", "nove",
    ];

    public static string Format(DetectedCall call, DestinationPronunciationMap? pronunciations = null)
    {
        ArgumentNullException.ThrowIfNull(call);
        ArgumentException.ThrowIfNullOrWhiteSpace(call.Identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(call.Destination);

        var identity = call.IdentityKind switch
        {
            IdentityKind.Name => call.Identity.Trim(),
            IdentityKind.Password => SpellPassword(call.Identity.Trim()),
            _ => throw new ArgumentOutOfRangeException(nameof(call), "Unsupported identity kind."),
        };
        var destination = pronunciations?.Find(call.Destination) ?? call.Destination.Trim();
        var sentenceEnd = destination.EndsWith('.') ? string.Empty : ".";

        return $"{identity}, dirija-se para {destination}{sentenceEnd}";
    }

    private static string SpellPassword(string password)
    {
        var spokenCharacters = new List<string>();
        foreach (var character in password.EnumerateRunes())
        {
            if (!Rune.IsLetterOrDigit(character))
            {
                throw new ArgumentException("A password must be alphanumeric.", nameof(password));
            }

            spokenCharacters.Add(Rune.ToUpperInvariant(character).Value switch
            {
                >= 'A' and <= 'Z' and var letter => LetterNames[letter - 'A'],
                >= '0' and <= '9' and var digit => DigitNames[digit - '0'],
                _ => character.ToString(),
            });
        }

        return string.Join(", ", spokenCharacters);
    }
}
