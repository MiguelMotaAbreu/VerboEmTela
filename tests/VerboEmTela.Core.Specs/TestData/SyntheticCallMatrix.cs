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

internal static class ReferenceDestinationCatalog
{
    public static IReadOnlyList<(string Sector, string Destination)> Routes { get; } =
        new[]
        {
            ("CLASSIFICAÇÃO", "SALA 1"),
            ("CLASSIFICAÇÃO", "SALA 2"),
            ("CLASSIFICAÇÃO", "SALA EXTRA"),
            ("CADASTRO", "RECEPÇÃO 1"),
            ("CADASTRO", "RECEPÇÃO 2"),
            ("CADASTRO", "RECEPÇÃO 3"),
            ("ATENDIMENTO MÉDICO", "SALA 3"),
            ("ATENDIMENTO MÉDICO", "SALA 4"),
            ("ATENDIMENTO MÉDICO", "SALA 5"),
            ("ATENDIMENTO MÉDICO", "SALA 6"),
            ("ATENDIMENTO MÉDICO", "SALA 7"),
            ("ATENDIMENTO MÉDICO", "SALA 8"),
            ("ATENDIMENTO MÉDICO", "ODONTOLOGIA"),
            ("ATENDIMENTO MÉDICO", "SALA EXTRA"),
        };

    public static bool Contains(string sector, string destination)
    {
        return Routes.Any(route =>
            string.Equals(route.Sector, sector, StringComparison.Ordinal) &&
            string.Equals(route.Destination, destination, StringComparison.Ordinal));
    }
}

internal static class SyntheticCallMatrix
{
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
        var route = ReferenceDestinationCatalog.Routes[
            (index - 1) % ReferenceDestinationCatalog.Routes.Count];
        var sector = route.Sector;
        var destination = route.Destination;
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
