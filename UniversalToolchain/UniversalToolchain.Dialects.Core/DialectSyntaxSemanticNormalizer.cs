using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Parsing;

namespace UniversalToolchain.Dialects.Core;

internal static class DialectSyntaxSemanticNormalizer
{
    public static DialectSyntaxNormalizationResult Normalize(DialectSyntaxDocument syntaxDocument, List<DialectDiagnostic> diagnostics)
    {
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

        return new DialectSyntaxNormalizationResult
        {
            ActiveModules = activeModules,
            BackendMap = backendMap,
            IntrinsicDirectives = intrinsicDirectives,
            OptimizerDirectives = optimizerDirectives,
            OrderConstraints = DialectOrderConstraintMapper.FromSyntaxRules(syntaxDocument.OrderRules)
        };
    }
}
