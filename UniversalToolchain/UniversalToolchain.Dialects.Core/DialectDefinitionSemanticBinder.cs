using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core.Binding;
using UniversalToolchain.Dialects.Core.Binding.Handlers;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Parsing;

namespace UniversalToolchain.Dialects.Core;

internal static class DialectDefinitionSemanticBinder
{
    private static readonly DialectDirectiveHandlerRegistry DirectiveHandlerRegistry = new(
    [
        new ModuleDirectiveHandler(),
        new BackendDirectiveHandler(),
        new IntrinsicDirectiveHandler(),
        new OptimizerDirectiveHandler(),
        new SecurityDirectiveHandler(),
        new CapabilityDirectiveHandler()
    ]);

    public static DialectDefinition Bind(DialectSyntaxDocument syntaxDocument, List<DialectDiagnostic> diagnostics)
    {
        if (syntaxDocument == null)
            Thrower.ArgumentNull(nameof(syntaxDocument));

        if (diagnostics == null)
            Thrower.ArgumentNull(nameof(diagnostics));

        return BindCore(new SyntaxDialectBindingSource(syntaxDocument), diagnostics);
    }

    public static DialectDefinition Bind(DialectDefinitionSlice compiledDialect, List<DialectDiagnostic> diagnostics)
    {
        if (compiledDialect == null)
            Thrower.ArgumentNull(nameof(compiledDialect));

        if (diagnostics == null)
            Thrower.ArgumentNull(nameof(diagnostics));

        return BindCore(new CompiledDialectBindingSource(compiledDialect), diagnostics);
    }

    internal static DialectDefinition BindCore(IDialectBindingSource source, List<DialectDiagnostic> diagnostics)
    {
        if (source == null)
            Thrower.ArgumentNull(nameof(source));

        if (diagnostics == null)
            Thrower.ArgumentNull(nameof(diagnostics));

        var builder = new DialectDefinitionBuilder();

        builder.SetIdentity(source.Name, source.Version, source.BaseDialectName);
        builder.SetOrderRules(DialectOrderConstraintMapper.ToDefinitionRules(DialectOrderConstraintMapper.FromBindingRules(source.OrderRules)));
        var context = new DialectBindingExecutionContext(source, builder, diagnostics);
        DirectiveHandlerRegistry.Apply(context);

        return builder.Build();
    }

    internal static DialectBuildPlan BuildPlanCore(
        IDialectBindingSource source,
        List<DialectDiagnostic> diagnostics,
        string cycleCode,
        string cycleMessagePrefix,
        string? missingReferenceCode = null,
        string? missingReferenceMessagePrefix = null)
    {
        if (source == null)
            Thrower.ArgumentNull(nameof(source));

        if (diagnostics == null)
            Thrower.ArgumentNull(nameof(diagnostics));

        if (string.IsNullOrWhiteSpace(cycleCode))
            Thrower.Argument(nameof(cycleCode), "Cycle diagnostic code must not be empty.");

        if (string.IsNullOrWhiteSpace(cycleMessagePrefix))
            Thrower.Argument(nameof(cycleMessagePrefix), "Cycle diagnostic message prefix must not be empty.");

        if (missingReferenceCode != null && string.IsNullOrWhiteSpace(missingReferenceCode))
            Thrower.Argument(nameof(missingReferenceCode), "Missing-reference diagnostic code must be null or non-empty.");

        if (missingReferenceMessagePrefix != null && string.IsNullOrWhiteSpace(missingReferenceMessagePrefix))
            Thrower.Argument(nameof(missingReferenceMessagePrefix), "Missing-reference diagnostic message prefix must be null or non-empty.");

        var definition = BindCore(source, diagnostics);

        return DialectDefinitionBuildPlanProjector.Project(
            definition,
            diagnostics,
            cycleCode,
            cycleMessagePrefix,
            missingReferenceCode,
            missingReferenceMessagePrefix);
    }
}
