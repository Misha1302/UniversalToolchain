using ExceptionsManager;
using UniversalToolchain.Diagnostics.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Rules;

public sealed class WistRuleBodyValidator
{
    public IReadOnlyList<ToolchainDiagnostic> Validate(IReadOnlyList<RuleDeclarationModel> rules)
    {
        rules = rules.ArgNotNull();

        var diagnostics = new List<ToolchainDiagnostic>();

        foreach (var rule in rules.OrderBy(static x => x.Name, StringComparer.Ordinal))
        {
            ValidateRuleParameters(rule, diagnostics);
            ValidateRuleLocals(rule, diagnostics);
        }

        ValidateDuplicateRuleNames(rules, diagnostics);
        return diagnostics;
    }

    private static void ValidateDuplicateRuleNames(IReadOnlyList<RuleDeclarationModel> rules, List<ToolchainDiagnostic> diagnostics)
    {
        foreach (var duplicateGroup in rules
                     .GroupBy(static x => x.Name, StringComparer.Ordinal)
                     .Where(static x => x.Count() > 1)
                     .OrderBy(static x => x.Key, StringComparer.Ordinal))
        {
            diagnostics.Add(CreateDiagnostic(ToolchainDiagnosticCodes.RuleDuplicateName, $"Duplicate rule declaration '{duplicateGroup.Key}'."));
        }
    }

    private static void ValidateRuleParameters(RuleDeclarationModel rule, List<ToolchainDiagnostic> diagnostics)
    {
        foreach (var duplicateGroup in rule.Parameters
                     .GroupBy(static x => x.Name, StringComparer.Ordinal)
                     .Where(static x => x.Count() > 1)
                     .OrderBy(static x => x.Key, StringComparer.Ordinal))
        {
            diagnostics.Add(CreateDiagnostic(ToolchainDiagnosticCodes.RuleDuplicateParameter, $"Duplicate parameter '{duplicateGroup.Key}' in rule '{rule.Name}'."));
        }
    }

    private static void ValidateRuleLocals(RuleDeclarationModel rule, List<ToolchainDiagnostic> diagnostics)
    {
        var parameterNames = rule.Parameters
            .Select(static x => x.Name)
            .ToHashSet(StringComparer.Ordinal);
        var scopedLocals = new Dictionary<RuleScopeId, HashSet<string>>();

        foreach (var localBinding in rule.Body.LocalBindings.OrderBy(static x => x.DeclarationOrder))
        {
            if (parameterNames.Contains(localBinding.Name))
            {
                diagnostics.Add(CreateDiagnostic(
                    ToolchainDiagnosticCodes.RuleLocalShadowsParameter,
                    $"Local binding '{localBinding.Name}' in rule '{rule.Name}' cannot shadow a rule parameter."));
                continue;
            }

            if (!scopedLocals.TryGetValue(localBinding.ScopeId, out var names))
            {
                names = new HashSet<string>(StringComparer.Ordinal);
                scopedLocals[localBinding.ScopeId] = names;
            }

            if (!names.Add(localBinding.Name))
            {
                diagnostics.Add(CreateDiagnostic(
                    ToolchainDiagnosticCodes.RuleDuplicateLocal,
                    $"Duplicate local binding '{localBinding.Name}' in rule '{rule.Name}'."));
            }
        }
    }

    private static ToolchainDiagnostic CreateDiagnostic(string code, string message)
    {
        return new ToolchainDiagnostic(code, ToolchainDiagnosticSeverity.Error, message, null, []);
    }
}
