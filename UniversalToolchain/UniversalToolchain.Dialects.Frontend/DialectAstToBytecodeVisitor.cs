using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectAstToBytecodeVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        if (data == null)
        {
            Thrower.ArgumentNull(nameof(data));
        }

        var document = DialectDslAstValidator.Validate(data.Node);
        var annotations = DialectAstLowering.Lower(document);
        data.Bytecode.Instructions.Add(new BytecodeInstruction(new DialectSliceToAirConvertable(annotations)));
    }
}

internal static class DialectAstLowering
{
    public static IReadOnlyList<object> Lower(DialectDocumentAstNode document)
    {
        if (document == null)
        {
            Thrower.ArgumentNull(nameof(document));
        }

        var annotations = new List<object>
        {
            new DialectNameAirAnnotation(document.Declaration.NameNode.Identifier)
        };

        foreach (var directive in document.Directives)
        {
            annotations.Add(LowerDirective(directive));
        }

        return annotations;
    }

    private static object LowerDirective(DialectDirectiveAstNode directive)
    {
        return directive switch
        {
            UseModulesDirectiveAstNode useDirective => new UseModulesAirAnnotation(GetIdentifiers(useDirective.Identifiers)),
            ExcludeModulesDirectiveAstNode excludeDirective => new ExcludeModulesAirAnnotation(GetIdentifiers(excludeDirective.Identifiers)),
            RequiresModulesDirectiveAstNode requiresDirective => new RequiresModulesAirAnnotation(GetIdentifiers(requiresDirective.Identifiers)),
            BeforeModulesDirectiveAstNode beforeDirective => new BeforeModulesAirAnnotation(GetIdentifiers(beforeDirective.Identifiers)),
            AfterModulesDirectiveAstNode afterDirective => new AfterModulesAirAnnotation(GetIdentifiers(afterDirective.Identifiers)),
            BackendDirectiveAstNode backendDirective => new BackendAirAnnotation(GetIdentifiers(backendDirective.Identifiers)),
            AllowIntrinsicDirectiveAstNode allowDirective => new AllowIntrinsicAirAnnotation(allowDirective.Identifier.Identifier),
            ForbidIntrinsicDirectiveAstNode forbidDirective => new ForbidIntrinsicAirAnnotation(forbidDirective.Identifier.Identifier),
            EnableIntrinsicDirectiveAstNode enableDirective => new EnableIntrinsicAirAnnotation(enableDirective.Identifier.Identifier),
            DisableIntrinsicDirectiveAstNode disableDirective => new DisableIntrinsicAirAnnotation(disableDirective.Identifier.Identifier),
            SecurityDirectiveAstNode securityDirective => new SecurityAirAnnotation(DialectAnnotationValueGuard.ParseSecurityProfile(securityDirective.Identifier.Identifier)),
            CapabilityDirectiveAstNode capabilityDirective => new CapabilityAirAnnotation(GetIdentifiers(capabilityDirective.Identifiers)),
            _ => Thrower.InvalidOpEx<object>($"Dialect lowering does not support AST node type '{directive.GetType().Name}'.")
        };
    }

    private static IReadOnlyList<string> GetIdentifiers(IdentifierListAstNode identifiers)
    {
        return identifiers.Identifiers.Select(x => x.Identifier).ToList();
    }
}
