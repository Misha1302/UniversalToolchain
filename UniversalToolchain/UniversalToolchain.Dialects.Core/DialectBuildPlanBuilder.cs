using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Parsing;
using ATarget = UniversalToolchain.Dialects.Abstractions.DialectBackendTarget;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Core;

/// <summary>
/// Default semantic validator and deterministic build-plan builder.
/// </summary>
public sealed class DialectBuildPlanBuilder : IDialectBuildPlanBuilder
{
    public DialectBuildPlan Build(DialectSyntaxDocument syntaxDocument)
    {
        if (syntaxDocument == null)
            Thrower.ArgumentNull(nameof(syntaxDocument));

        var diagnostics = new List<DialectDiagnostic>();

        var activeModules = DialectSemanticNormalization.NormalizeActiveModules(
            syntaxDocument.UseModules,
            syntaxDocument.ExcludeModules,
            diagnostics,
            conflictCode: "S001");

        var backendMap = DialectSemanticNormalization.NormalizeBackendRules(
            syntaxDocument.BackendDirectives,
            x => x.Backend,
            x => x.Enabled,
            diagnostics,
            contradictionCode: "S003");

        var intrinsicDirectives = DialectSemanticNormalization.NormalizeIntrinsicRules(
            syntaxDocument.IntrinsicDirectives,
            x => x.Name,
            x => x.Target,
            x => x.Allowed,
            diagnostics,
            contradictionCode: "S004");

        var optimizerDirectives = DialectSemanticNormalization.NormalizeOptimizerRules(
            syntaxDocument.OptimizerDirectives,
            x => x.Name,
            x => x.Target,
            x => x.Enabled,
            diagnostics,
            contradictionCode: "S005");

        ValidateSecurityCapabilities(syntaxDocument, diagnostics);

        var orderedModules = DialectSemanticNormalization.ResolveOrder(
            activeModules,
            syntaxDocument.OrderRules.Select(ToOrderConstraint).ToList(),
            diagnostics,
            cycleCode: "S007",
            cycleMessagePrefix: "Order rules contain a cycle involving modules",
            missingReferenceCode: "S002",
            missingReferenceMessagePrefix: "Order rule references module(s) not present in active module set");

        var validationResult = new DialectValidationResult(diagnostics);

        return new DialectBuildPlan(
            syntaxDocument.Name,
            syntaxDocument.Version,
            orderedModules,
            backendMap.Where(x => x.Value).Select(x => DialectBackendTargetText.ToText(x.Key)).OrderBy(x => x, StringComparer.Ordinal).ToList(),
            backendMap.Where(x => !x.Value).Select(x => DialectBackendTargetText.ToText(x.Key)).OrderBy(x => x, StringComparer.Ordinal).ToList(),
            intrinsicDirectives,
            optimizerDirectives,
            syntaxDocument.SecurityProfile,
            syntaxDocument.Capabilities.OrderBy(x => x.Key, StringComparer.Ordinal).ToList(),
            validationResult);
    }

    private static void ValidateSecurityCapabilities(DialectSyntaxDocument syntaxDocument, List<DialectDiagnostic> diagnostics)
    {
        if (syntaxDocument.SecurityProfile != SecurityProfile.Restricted)
            return;

        if (syntaxDocument.Capabilities.TryGetValue("unsafe-interop", out var enabled) && enabled)
        {
            diagnostics.Add(new DialectDiagnostic(
                "S006",
                "Capability 'unsafe-interop' cannot be enabled under restricted security profile.",
                DialectDiagnosticSeverity.Error));
        }
    }

    private static DialectOrderConstraint ToOrderConstraint(OrderRule rule)
    {
        var kind = rule.Kind switch
        {
            OrderRuleKind.Before => DialectOrderConstraintKind.Before,
            OrderRuleKind.After => DialectOrderConstraintKind.After,
            _ => DialectOrderConstraintKind.Requires,
        };

        return new DialectOrderConstraint(kind, rule.ModuleName, rule.RelatedModuleName);
    }
}
