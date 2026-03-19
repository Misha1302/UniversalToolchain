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