using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectAstToBytecodeVisitor : IAstVisitor
{
    private readonly DialectDslRegistry _registry;

    public DialectAstToBytecodeVisitor(DialectDslRegistry? registry = null)
    {
        _registry = registry ?? DialectDslBuiltInFeatures.CreateRegistry();
    }

    public void TryVisit(BytecodeVisitorData data)
    {
        if (data == null)
        {
            Thrower.ArgumentNull(nameof(data));
        }

        var document = DialectDslAstValidator.Validate(data.Node, _registry);
        var annotations = DialectAstLowering.Lower(document);
        data.Bytecode.Instructions.Add(new BytecodeInstruction(new DialectSliceToAirConvertable(annotations.Cast<object>().ToList())));
    }
}

internal static class DialectAstLowering
{
    public static IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDocumentAstNode document)
    {
        if (document == null)
        {
            Thrower.ArgumentNull(nameof(document));
        }

        var annotations = new List<IDialectDefinitionSliceAnnotation>
        {
            new DialectNameAirAnnotation(document.Declaration.NameNode.Identifier)
        };

        foreach (var directive in document.Directives)
        {
            annotations.AddRange(directive.Feature.Lower(directive));
        }

        return annotations;
    }
}
