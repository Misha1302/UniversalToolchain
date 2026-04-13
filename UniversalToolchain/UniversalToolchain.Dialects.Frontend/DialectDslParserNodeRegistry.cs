using BasicCore.ParserWrapper;
using BasicCore.Registration;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectDslParserNodeRegistry
{
    public static IReadOnlyList<NodeCreatorRegistration> CreateRegistrations(DialectDslRegistry registry)
    {
        registry = registry.ArgNotNull();

        var entries = new List<(DialectParserOrder Order, IAstNodeCreator Creator, string Description)>
        {
            (DialectParserOrders.LineSplitter, new DialectLineNodeCreator(), nameof(DialectLineNodeCreator)),
            (DialectParserOrders.Declaration, new DialectDeclarationNodeCreator(), nameof(DialectDeclarationNodeCreator))
        };

        entries.AddRange(registry.DirectiveFeatures.Select(x => (DialectParserOrder.Directive(x.ParserOrder), (IAstNodeCreator)new FeatureDialectDirectiveNodeCreator(x), $"{x.Id}:{x.Keyword}")));
        entries.Add((DialectParserOrders.Document, new DialectDocumentNodeCreator(), nameof(DialectDocumentNodeCreator)));

        var orderedEntries = entries
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Description, StringComparer.Ordinal)
            .ToList();

        DialectParserOrderValidation.EnsureNoCollisions(orderedEntries, static x => x.Order, static x => x.Description, "parser node creators");

        return orderedEntries
            .Select((entry, index) => new NodeCreatorRegistration(index, entry.Creator))
            .ToList();
    }
}