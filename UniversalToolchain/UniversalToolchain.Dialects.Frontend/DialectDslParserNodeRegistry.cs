using BasicCore.Registration;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectDslParserNodeRegistry
{
    public static IReadOnlyList<NodeCreatorRegistration> CreateRegistrations(DialectDslRegistry registry)
    {
        if (registry == null)
        {
            Thrower.ArgumentNull(nameof(registry));
        }

        var registrations = new List<NodeCreatorRegistration>
        {
            new(DialectParserOrders.LineSplitter.Encode(), new DialectLineNodeCreator()),
            new(DialectParserOrders.Declaration.Encode(), new DialectDeclarationNodeCreator())
        };

        registrations.AddRange(registry.DirectiveFeatures.Select(x => new NodeCreatorRegistration(DialectParserOrder.Directive(x.ParserOrder).Encode(), new FeatureDialectDirectiveNodeCreator(x))));
        registrations.Add(new NodeCreatorRegistration(DialectParserOrders.Document.Encode(), new DialectDocumentNodeCreator()));
        return registrations;
    }
}
