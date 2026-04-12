using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core.Binding;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Parsing;

namespace UniversalToolchain.Dialects.Core;

internal static class DialectDefinitionSemanticBinder
{
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

        var intrinsicDirectives = DialectSemanticNormalization.NormalizeIntrinsicRules(
            source.IntrinsicDirectives,
            x => x.Name,
            x => x.Target,
            x => x.Allowed,
            diagnostics,
            diagnosticCodes.IntrinsicContradiction);

        var optimizerDirectives = DialectSemanticNormalization.NormalizeOptimizerRules(
            source.OptimizerDirectives,
            x => x.Name,
            x => x.Target,
            x => x.Enabled,
            diagnostics,
            diagnosticCodes.OptimizerContradiction);

        builder.SetModulePolicy(new ModulePolicy(
            activeModules,
            source.ExcludeModules.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal)));
        builder.SetBackendPolicy(new BackendPolicy(
            backendMap.Where(x => x.Value).Select(x => x.Key).OrderBy(x => x, Comparer<DialectBackendId>.Default),
            backendMap.Where(x => !x.Value).Select(x => x.Key).OrderBy(x => x, Comparer<DialectBackendId>.Default)));
        builder.SetIntrinsicPolicy(new IntrinsicPolicy(
            intrinsicDirectives.Where(x => x.Allowed).Select(FormatRuleName),
            intrinsicDirectives.Where(x => !x.Allowed).Select(FormatRuleName)));
        builder.SetOptimizerPolicy(new OptimizerPolicy(
            optimizerDirectives.Where(x => x.Enabled).Select(FormatRuleName),
            optimizerDirectives.Where(x => !x.Enabled).Select(FormatRuleName)));
        builder.SetSecurityPolicy(source.SecurityProfile.HasValue ? new SecurityPolicy(source.SecurityProfile.Value) : null);
        builder.SetCapabilityPolicy(new CapabilityPolicy(source.Capabilities.OrderBy(x => x.Key, StringComparer.Ordinal)));
        builder.SetOrderRules(DialectOrderConstraintMapper.ToDefinitionRules(DialectOrderConstraintMapper.FromBindingRules(source.OrderRules)));

        return builder.Build();
    }

    private static string FormatRuleName(IntrinsicBuildDirective directive) => FormatRuleName(directive.Name, directive.Target);

    private static string FormatRuleName(OptimizerBuildDirective directive) => FormatRuleName(directive.Name, directive.Target);

    private static string FormatRuleName(string name, DialectBackendSelector target) => target.IsAny ? name : $"{name}@{DialectBackendSelectorText.ToText(target.BackendId)}";

    private static BindingDiagnosticCodes GetDiagnosticCodes(DialectBindingInputKind inputKind)
    {
        return inputKind switch
        {
            DialectBindingInputKind.Compiled => new BindingDiagnosticCodes("S101", "S102", "S103", "S104"),
            _ => new BindingDiagnosticCodes("S001", "S003", "S004", "S005")
        };
    }

    private readonly record struct BindingDiagnosticCodes(
        string ModuleConflict,
        string BackendContradiction,
        string IntrinsicContradiction,
        string OptimizerContradiction);
}
