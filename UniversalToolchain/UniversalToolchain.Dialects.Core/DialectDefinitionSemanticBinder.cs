using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;
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

        var activeModules = DialectSemanticNormalization.NormalizeActiveModules(
            syntaxDocument.UseModules,
            syntaxDocument.ExcludeModules,
            diagnostics,
            "S001");

        var backendMap = DialectSemanticNormalization.NormalizeBackendRules(
            syntaxDocument.BackendDirectives,
            x => x.Backend,
            x => x.Enabled,
            diagnostics,
            "S003");

        var intrinsicDirectives = DialectSemanticNormalization.NormalizeIntrinsicRules(
            syntaxDocument.IntrinsicDirectives,
            x => x.Name,
            x => x.Target,
            x => x.Allowed,
            diagnostics,
            "S004");

        var optimizerDirectives = DialectSemanticNormalization.NormalizeOptimizerRules(
            syntaxDocument.OptimizerDirectives,
            x => x.Name,
            x => x.Target,
            x => x.Enabled,
            diagnostics,
            "S005");

        return new DialectDefinition(
            syntaxDocument.Name,
            new ModulePolicy(activeModules, syntaxDocument.ExcludeModules.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal)),
            new BackendPolicy(
                backendMap.Where(x => x.Value).Select(x => x.Key).OrderBy(x => x),
                backendMap.Where(x => !x.Value).Select(x => x.Key).OrderBy(x => x)),
            new IntrinsicPolicy(
                intrinsicDirectives.Where(x => x.Allowed).Select(FormatRuleName),
                intrinsicDirectives.Where(x => !x.Allowed).Select(FormatRuleName)),
            new OptimizerPolicy(
                optimizerDirectives.Where(x => x.Enabled).Select(FormatRuleName),
                optimizerDirectives.Where(x => !x.Enabled).Select(FormatRuleName)),
            syntaxDocument.SecurityProfile.HasValue ? new SecurityPolicy(syntaxDocument.SecurityProfile.Value) : null,
            new CapabilityPolicy(syntaxDocument.Capabilities.OrderBy(x => x.Key, StringComparer.Ordinal)),
            DialectOrderConstraintMapper.ToDefinitionRules(DialectOrderConstraintMapper.FromSyntaxRules(syntaxDocument.OrderRules)),
            syntaxDocument.Version);
    }

    public static DialectDefinition Bind(DialectDefinitionSlice compiledDialect, List<DialectDiagnostic> diagnostics)
    {
        if (compiledDialect == null)
            Thrower.ArgumentNull(nameof(compiledDialect));

        if (diagnostics == null)
            Thrower.ArgumentNull(nameof(diagnostics));

        var activeModules = DialectSemanticNormalization.NormalizeActiveModules(
            compiledDialect.UseModules,
            compiledDialect.ExcludeModules,
            diagnostics,
            "S101");

        var backendMap = DialectSemanticNormalization.NormalizeBackendRules(
            compiledDialect.BackendDirectives,
            x => x.Backend,
            x => x.Enabled,
            diagnostics,
            "S102");

        var intrinsicDirectives = DialectSemanticNormalization.NormalizeIntrinsicRules(
            compiledDialect.IntrinsicDirectives,
            x => x.Name,
            x => x.Target,
            x => x.Allowed,
            diagnostics,
            "S103");

        var optimizerDirectives = DialectSemanticNormalization.NormalizeOptimizerRules(
            compiledDialect.OptimizerDirectives,
            x => x.Name,
            x => x.Target,
            x => x.Enabled,
            diagnostics,
            "S104");

        var capabilities = compiledDialect.CapabilityDirectives
            .OrderBy(x => x.Name, StringComparer.Ordinal)
            .Select(x => new KeyValuePair<string, bool>(x.Name, x.Value));

        return new DialectDefinition(
            compiledDialect.Name,
            new ModulePolicy(activeModules, compiledDialect.ExcludeModules.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal)),
            new BackendPolicy(
                backendMap.Where(x => x.Value).Select(x => x.Key).OrderBy(x => x),
                backendMap.Where(x => !x.Value).Select(x => x.Key).OrderBy(x => x)),
            new IntrinsicPolicy(
                intrinsicDirectives.Where(x => x.Allowed).Select(FormatRuleName),
                intrinsicDirectives.Where(x => !x.Allowed).Select(FormatRuleName)),
            new OptimizerPolicy(
                optimizerDirectives.Where(x => x.Enabled).Select(FormatRuleName),
                optimizerDirectives.Where(x => !x.Enabled).Select(FormatRuleName)),
            compiledDialect.SecurityProfile.HasValue ? new SecurityPolicy(ToSecurityProfile(compiledDialect.SecurityProfile.Value)) : null,
            new CapabilityPolicy(capabilities),
            DialectOrderConstraintMapper.ToDefinitionRules(DialectOrderConstraintMapper.FromCompiledDirectives(compiledDialect.OrderDirectives)));
    }

    private static string FormatRuleName(IntrinsicBuildDirective directive) => FormatRuleName(directive.Name, directive.Target);

    private static string FormatRuleName(OptimizerBuildDirective directive) => FormatRuleName(directive.Name, directive.Target);

    private static string FormatRuleName(string name, DialectBackendTarget target) => target == DialectBackendTarget.Any ? name : $"{name}@{DialectBackendTargetText.ToText(target)}";

    private static SecurityProfile ToSecurityProfile(DialectSecurityProfile profile)
    {
        return profile switch
        {
            DialectSecurityProfile.Trusted => SecurityProfile.Trusted,
            _ => SecurityProfile.Restricted
        };
    }
}