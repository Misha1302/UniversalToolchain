using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Parsing;
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
        var normalized = DialectSyntaxSemanticNormalizer.Normalize(syntaxDocument, diagnostics);

        DialectSecurityCapabilityPolicyValidator.Validate(
            syntaxDocument.SecurityProfile,
            syntaxDocument.Capabilities,
            diagnostics);

        var orderedModules = DialectSemanticNormalization.ResolveOrder(
            normalized.ActiveModules,
            normalized.OrderConstraints,
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
            normalized.BackendMap.Where(x => x.Value).Select(x => x.Key).OrderBy(x => x).ToList(),
            normalized.BackendMap.Where(x => !x.Value).Select(x => x.Key).OrderBy(x => x).ToList(),
            normalized.IntrinsicDirectives,
            normalized.OptimizerDirectives,
            syntaxDocument.SecurityProfile,
            syntaxDocument.Capabilities.OrderBy(x => x.Key, StringComparer.Ordinal).ToList(),
            validationResult);
    }
}
