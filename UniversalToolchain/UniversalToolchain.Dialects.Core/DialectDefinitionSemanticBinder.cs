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

        var diagnosticCodes = GetDiagnosticCodes(source.InputKind);
        var builder = new DialectDefinitionBuilder();

        builder.SetIdentity(source.Name, source.Version, source.BaseDialectName);

        var activeModules = DialectSemanticNormalization.NormalizeActiveModules(
            source.UseModules,
            source.ExcludeModules,
            diagnostics,
            diagnosticCodes.ModuleConflict);

        var backendMap = DialectSemanticNormalization.NormalizeBackendRules(
            source.BackendDirectives,
            x => x.Backend,
            x => x.Enabled,
            diagnostics,
            diagnosticCodes.BackendContradiction);

        builder.SetModulePolicy(new ModulePolicy(
            activeModules,
            source.ExcludeModules.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal)));
        builder.SetBackendPolicy(new BackendPolicy(
            backendMap.Where(x => x.Value).Select(x => x.Key).OrderBy(x => x, Comparer<DialectBackendId>.Default),
            backendMap.Where(x => !x.Value).Select(x => x.Key).OrderBy(x => x, Comparer<DialectBackendId>.Default)));
        builder.SetOrderRules(DialectOrderConstraintMapper.ToDefinitionRules(DialectOrderConstraintMapper.FromBindingRules(source.OrderRules)));
        DirectiveHandlerRegistry.Apply(source, builder, diagnostics);

        return builder.Build();
    }

    private static BindingDiagnosticCodes GetDiagnosticCodes(DialectBindingInputKind inputKind)
    {
        return inputKind switch
        {
            DialectBindingInputKind.Compiled => new BindingDiagnosticCodes("S101", "S102"),
            _ => new BindingDiagnosticCodes("S001", "S003")
        };
    }

    private readonly record struct BindingDiagnosticCodes(
        string ModuleConflict,
        string BackendContradiction);
}
