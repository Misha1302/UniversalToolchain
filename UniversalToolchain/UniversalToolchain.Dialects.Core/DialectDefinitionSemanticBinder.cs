using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core.Binding;
using UniversalToolchain.Dialects.Core.Binding.Handlers;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Parsing;

namespace UniversalToolchain.Dialects.Core;

internal static class DialectDefinitionSemanticBinder
{
    private static readonly DialectDirectiveHandlerRegistry _defaultDirectiveHandlerRegistry = CreateDefaultDirectiveHandlerRegistry();

    public static DialectDefinition Bind(DialectSyntaxDocument syntaxDocument, List<DialectDiagnostic> diagnostics)
    {
        syntaxDocument = syntaxDocument.ArgNotNull();
        diagnostics = diagnostics.ArgNotNull();
        return BindCore(new SyntaxDialectBindingSource(syntaxDocument), diagnostics, _defaultDirectiveHandlerRegistry);
    }

    public static DialectDefinition Bind(DialectDefinitionSlice compiledDialect, List<DialectDiagnostic> diagnostics)
    {
        compiledDialect = compiledDialect.ArgNotNull();
        diagnostics = diagnostics.ArgNotNull();
        return BindCore(new CompiledDialectBindingSource(compiledDialect), diagnostics, _defaultDirectiveHandlerRegistry);
    }

    internal static DialectDefinition BindCore(IDialectBindingSource source, List<DialectDiagnostic> diagnostics) =>
        BindCore(source, diagnostics, _defaultDirectiveHandlerRegistry);

    internal static DialectDefinition BindCore(
        IDialectBindingSource source,
        List<DialectDiagnostic> diagnostics,
        DialectDirectiveHandlerRegistry directiveHandlerRegistry)
    {
        source = source.ArgNotNull();
        diagnostics = diagnostics.ArgNotNull();
        directiveHandlerRegistry = directiveHandlerRegistry.ArgNotNull();

        var builder = new DialectDefinitionBuilder();
        builder.SetIdentity(source.Name, source.Version, source.BaseDialectName);
        builder.SetOrderRules(DialectOrderConstraintMapper.ToDefinitionRules(
            DialectOrderConstraintMapper.FromBindingRules(source.OrderRules)));
        var context = new DialectDirectiveBindingContext(source, builder, diagnostics);
        directiveHandlerRegistry.Apply(context);
        return builder.Build();
    }

    internal static DialectDirectiveHandlerRegistry CreateDefaultDirectiveHandlerRegistry() => new(
    [
        new ModuleDirectiveHandler(),
        new BackendDirectiveHandler(),
        new IntrinsicDirectiveHandler(),
        new OptimizerDirectiveHandler(),
        new SecurityDirectiveHandler(),
        new CapabilityDirectiveHandler()
    ]);
}
