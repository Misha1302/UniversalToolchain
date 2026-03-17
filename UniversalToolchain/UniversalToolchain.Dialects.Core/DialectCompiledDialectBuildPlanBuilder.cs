using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Frontend;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Core;

/// <summary>
/// Builds a validated and normalized DialectBuildPlan from framework-native compiled DSL output.
/// </summary>
public sealed class DialectCompiledDialectBuildPlanBuilder : IDialectCompiledDialectBuildPlanBuilder
{
    public DialectBuildPlan Build(DialectDefinitionSlice compiledDialect)
    {
        if (compiledDialect == null)
        {
            Thrower.ArgumentNull(nameof(compiledDialect));
        }

        var diagnostics = new List<DialectDiagnostic>();

        var activeModules = DialectSemanticNormalization.NormalizeActiveModules(
            compiledDialect.UseModules,
            compiledDialect.ExcludeModules,
            diagnostics,
            conflictCode: "S101");

        var backendMap = DialectSemanticNormalization.NormalizeBackendRules(
            compiledDialect.BackendDirectives,
            x => x.Backend,
            x => x.Enabled,
            diagnostics,
            contradictionCode: "S102");

        var intrinsicDirectives = DialectSemanticNormalization.NormalizeIntrinsicRules(
            compiledDialect.IntrinsicDirectives,
            x => x.Name,
            x => x.Target,
            x => x.Allowed,
            diagnostics,
            contradictionCode: "S103");

        var optimizerDirectives = DialectSemanticNormalization.NormalizeOptimizerRules(
            compiledDialect.OptimizerDirectives,
            x => x.Name,
            x => x.Target,
            x => x.Enabled,
            diagnostics,
            contradictionCode: "S104");

        var orderedModules = DialectSemanticNormalization.ResolveOrder(
            activeModules,
            DialectOrderConstraintMapper.FromCompiledDirectives(compiledDialect.OrderDirectives),
            diagnostics,
            cycleCode: "S105",
            cycleMessagePrefix: "Order directives contain a cycle involving modules");

        var capabilities = compiledDialect.CapabilityDirectives
            .OrderBy(x => x.Name, StringComparer.Ordinal)
            .Select(x => new KeyValuePair<string, bool>(x.Name, x.Value))
            .ToList();

        var validationResult = new DialectValidationResult(diagnostics);

        return new DialectBuildPlan(
            compiledDialect.Name,
            version: null,
            orderedModules,
            enabledBackends: backendMap.Where(x => x.Value).Select(x => x.Key).OrderBy(x => x).ToList(),
            disabledBackends: backendMap.Where(x => !x.Value).Select(x => x.Key).OrderBy(x => x).ToList(),
            intrinsicDirectives,
            optimizerDirectives,
            securityProfile: ToSecurityProfile(compiledDialect.SecurityProfile),
            capabilities,
            validationResult);
    }

    private static SecurityProfile? ToSecurityProfile(DialectSecurityProfile? profile)
    {
        return profile switch
        {
            null => null,
            DialectSecurityProfile.Trusted => SecurityProfile.Trusted,
            _ => SecurityProfile.Restricted
        };
    }
}
