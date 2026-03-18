using BasicCore.ParserWrapper;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDefinitionSliceParser
{
    public DialectDefinitionSlice Parse(AstNode astRoot)
    {
        if (astRoot == null)
        {
            Thrower.ArgumentNull(nameof(astRoot));
        }

        var document = DialectDslAstValidator.Validate(astRoot);
        var annotations = DialectAstLowering.Lower(document);
        var aggregation = new DialectDefinitionAggregation();
        foreach (var annotation in annotations)
        {
            annotation.Apply(aggregation);
        }

        return new DialectDefinitionSliceBuilder().Build(aggregation);
    }
}
