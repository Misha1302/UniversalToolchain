using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using ExceptionsManager;
using UniversalToolchain.Dialects.Frontend.Composition;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectAstToBytecodeVisitor : IAstVisitor
{
    private readonly DialectDslRegistry _registry;

    public DialectAstToBytecodeVisitor(DialectDslRegistry registry)
    {
        registry = registry.ArgNotNull();

        _registry = registry;
    }

    public void TryVisit(BytecodeVisitorData data)
    {
        data = data.ArgNotNull();

        var document = DialectDslAstValidator.Validate(data.Node, _registry);
        var annotations = DialectAstLowering.Lower(document);
        data.Bytecode.Instructions.Add(new BytecodeInstruction(new DialectSliceToAirConvertable(annotations.Cast<object>().ToList())));
    }
}

internal static class DialectAstLowering
{
    public static IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDocumentAstNode document)
    {
        document = document.ArgNotNull();

        var annotations = new List<IDialectDefinitionSliceAnnotation>
        {
            new DialectNameAirAnnotation(document.Declaration.NameNode.Identifier)
        };

        foreach (var directive in document.Directives)
            annotations.AddRange(directive.Feature.Lower(directive));

        return annotations;
    }
}