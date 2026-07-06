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

    internal static DialectDefinition BindCore(IDialectBindingSource source, List<DialectDiagnostic> diagnostics)
        => BindCore(source, diagnostics, _defaultDirectiveHandlerRegistry);

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
        builder.SetOrderRules(DialectOrderConstraintMapper.ToDefinitionRules(DialectOrderConstraintMapper.FromBindingRules(source.OrderRules)));
        var context = new DialectDirectiveBindingContext(source, builder, diagnostics);
        directiveHandlerRegistry.Apply(context);

        return builder.Build();
    }

    internal static DialectBuildPlan BuildPlanCore(
        IDialectBindingSource source,
        List<DialectDiagnostic> diagnostics,
        string cycleCode,
        string cycleMessagePrefix,
        string? missingReferenceCode = null,
        string? missingReferenceMessagePrefix = null)
        => BuildPlanCore(
            source,
            diagnostics,
            cycleCode,
            cycleMessagePrefix,
            _defaultDirectiveHandlerRegistry,
            missingReferenceCode,
            missingReferenceMessagePrefix);

    internal static DialectBuildPlan BuildPlanCore(
        IDialectBindingSource source,
        List<DialectDiagnostic> diagnostics,
        string cycleCode,
        string cycleMessagePrefix,
        DialectDirectiveHandlerRegistry directiveHandlerRegistry,
        string? missingReferenceCode = null,
        string? missingReferenceMessagePrefix = null)
    {
        source = source.ArgNotNull();

        diagnostics = diagnostics.ArgNotNull();
        directiveHandlerRegistry = directiveHandlerRegistry.ArgNotNull();

        if (string.IsNullOrWhiteSpace(cycleCode))
            Thrower.Argument(nameof(cycleCode), "Cycle diagnostic code must not be empty.");

        if (string.IsNullOrWhiteSpace(cycleMessagePrefix))
            Thrower.Argument(nameof(cycleMessagePrefix), "Cycle diagnostic message prefix must not be empty.");

        if (missingReferenceCode != null && string.IsNullOrWhiteSpace(missingReferenceCode))
            Thrower.Argument(nameof(missingReferenceCode), "Missing-reference diagnostic code must be null or non-empty.");

        if (missingReferenceMessagePrefix != null && string.IsNullOrWhiteSpace(missingReferenceMessagePrefix))
            Thrower.Argument(nameof(missingReferenceMessagePrefix), "Missing-reference diagnostic message prefix must be null or non-empty.");

        var definition = BindCore(source, diagnostics, directiveHandlerRegistry);

        return DialectDefinitionBuildPlanProjector.Project(
            definition,
            diagnostics,
            cycleCode,
            cycleMessagePrefix,
            missingReferenceCode,
            missingReferenceMessagePrefix);
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
