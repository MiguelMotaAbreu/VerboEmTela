using VerboEmTela.Core.Detection;

namespace VerboEmTela.Core.Specs.TestData;

internal enum SyntheticEventKind
{
    NameCall,
    PasswordOnlyCall,
    RecallAfterDashboard,
    RapidConsecutiveCall,
    MappedDestination,
    LiteralDestination,
    LongName,
    AccentedName,
    IncompleteTransition,
    InvalidScreen,
}

internal enum SyntheticExpectedOutcome
{
    Detect,
    Reject,
}

internal sealed record SyntheticCallCase(
    string Id,
    SyntheticEventKind Kind,
    string? Sector,
    string? Name,
    string? Password,
    string? Destination,
    SyntheticExpectedOutcome ExpectedOutcome)
{
    public IdentityKind? ExpectedIdentityKind =>
        Name is not null
            ? IdentityKind.Name
            : Password is not null
                ? IdentityKind.Password
                : null;

    public string? ExpectedIdentity => Name ?? Password;
}

internal static class SyntheticCallMatrix
{
    private static readonly string[] Sectors =
    {
        "RECEPÇÃO",
        "CLASSIFICAÇÃO",
        "ODONTOLOGIA",
    };

    private static readonly string[] Destinations =
    {
        "CONSULTÓRIO 1",
        "CONSULTÓRIO 3",
        "RECEPÇÃO 3",
        "CLASSIFICAÇÃO",
        "ODONTOLOGIA",
        "SALA DE PROCEDIMENTOS",
    };

    public static IReadOnlyList<SyntheticCallCase> Create()
    {
        var cases = new List<SyntheticCallCase>(100);
        var kinds = Enum.GetValues<SyntheticEventKind>();

        for (var index = 1; index <= 100; index++)
        {
            var kind = kinds[(index - 1) % kinds.Length];
            cases.Add(CreateCase(index, kind));
        }

        return cases;
    }

    private static SyntheticCallCase CreateCase(
        int index,
        SyntheticEventKind kind)
    {
        var id = $"SYN-{index:000}";
        var sector = Sectors[(index - 1) % Sectors.Length];
        var destination = Destinations[(index - 1) % Destinations.Length];
        var name = $"PESSOA FICTÍCIA TESTE {index:000}";
        string? password = null;
        var expectedOutcome = SyntheticExpectedOutcome.Detect;

        switch (kind)
        {
            case SyntheticEventKind.PasswordOnlyCall:
                name = null;
                password = $"TST{index:0000}";
                break;

            case SyntheticEventKind.MappedDestination:
                destination = "CONSULT. 03";
                break;

            case SyntheticEventKind.LiteralDestination:
                destination = "SALA DE OBSERVAÇÃO PEDIÁTRICA";
                break;

            case SyntheticEventKind.LongName:
                name = $"PESSOA FICTÍCIA NOME EXTENSO COMPOSTO TESTE {index:000}";
                break;

            case SyntheticEventKind.AccentedName:
                name = $"PESSOA FICTÍCIA ÁGATA ÇÊ TESTE {index:000}";
                break;

            case SyntheticEventKind.IncompleteTransition:
                destination = null;
                expectedOutcome = SyntheticExpectedOutcome.Reject;
                break;

            case SyntheticEventKind.InvalidScreen:
                sector = null;
                name = null;
                destination = null;
                expectedOutcome = SyntheticExpectedOutcome.Reject;
                break;
        }

        return new SyntheticCallCase(
            id,
            kind,
            sector,
            name,
            password,
            destination,
            expectedOutcome);
    }
}
