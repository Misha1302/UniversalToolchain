using BasicCore.ParserWrapper;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDefinitionSliceParser
{
    private readonly DialectDslRegistry _registry;

    public DialectDefinitionSliceParser(DialectDslRegistry registry)
    {
        if (registry == null)
            Thrower.ArgumentNull(nameof(registry));

        _registry = registry;
    }

    public DialectDefinitionSlice Parse(AstNode astRoot)
    {
        if (astRoot == null)
            Thrower.ArgumentNull(nameof(astRoot));

        var document = DialectDslAstValidator.Validate(astRoot, _registry);
        var annotations = DialectAstLowering.Lower(document);
        var aggregation = new DialectDefinitionAggregation();
        foreach (var annotation in annotations)
            annotation.Apply(aggregation);

        return new DialectDefinitionSliceBuilder().Build(aggregation);
    }
}