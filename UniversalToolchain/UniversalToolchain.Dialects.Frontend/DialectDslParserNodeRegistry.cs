using BasicCore.Registration;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectDslParserNodeRegistry
{
    private static readonly Lazy<IReadOnlyList<NodeCreatorRegistration>> _registrations = new(BuildRegistrations);

    public static IReadOnlyList<NodeCreatorRegistration> Registrations => _registrations.Value;

    private static IReadOnlyList<NodeCreatorRegistration> BuildRegistrations()
    {
        var registrations = new List<NodeCreatorRegistration>
        {
            new(0f, new DialectLineNodeCreator()),
            new(10f, new DialectDeclarationNodeCreator())
        };

        registrations.AddRange(DialectDslFeatureCatalog.Features.Select(x => new NodeCreatorRegistration(x.ParserPriority, x.CreateNodeCreator())));
        registrations.Add(new NodeCreatorRegistration(100f, new DialectDocumentNodeCreator()));
        return registrations;
    }
}
