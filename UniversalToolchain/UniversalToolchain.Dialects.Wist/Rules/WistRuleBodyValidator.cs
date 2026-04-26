using ExceptionsManager;
using System.Text.RegularExpressions;
using UniversalToolchain.Diagnostics.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Rules;

public sealed class WistRuleBodyValidator
{
    private static readonly Regex LetBindingRegex = new(
        @"^\s*let\s+([A-Za-z_][A-Za-z0-9_]*)\s*=",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public IReadOnlyList<ToolchainDiagnostic> Validate(IReadOnlyList<RuleDeclarationModel> rules)
    {
        rules = rules.ArgNotNull();

        var diagnostics = new List<ToolchainDiagnostic>();
        foreach (var rule in rules.OrderBy(static x => x.Name, StringComparer.Ordinal))
            ValidateRule(rule, diagnostics);

        return diagnostics;
    }

    private static void ValidateRule(RuleDeclarationModel rule, List<ToolchainDiagnostic> diagnostics)
    {
        var parameterNames = rule.Parameters
            .Select(static x => x.Name)
            .ToHashSet(StringComparer.Ordinal);
        var localNames = new HashSet<string>(StringComparer.Ordinal);
        var lines = rule.Body.Split('\n');

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            var match = LetBindingRegex.Match(line);
            if (!match.Success)
                continue;

            var localName = match.Groups[1].Value;
            if (string.IsNullOrWhiteSpace(localName))
            {
                diagnostics.Add(CreateDiagnostic(
                    ToolchainDiagnosticCodes.RuleInvalidBody,
                    $"Local binding declaration in rule '{rule.Name}' has an empty name at body line {lineIndex + 1}."));
                continue;
            }

            if (parameterNames.Contains(localName))
            {
                diagnostics.Add(CreateDiagnostic(
                    ToolchainDiagnosticCodes.RuleLocalShadowsParameter,
                    $"Local binding '{localName}' in rule '{rule.Name}' cannot shadow a rule parameter."));
                continue;
            }

            if (!localNames.Add(localName))
            {
                diagnostics.Add(CreateDiagnostic(
                    ToolchainDiagnosticCodes.RuleDuplicateLocal,
                    $"Duplicate local binding '{localName}' in rule '{rule.Name}'."));
            }
        }
    }

    private static ToolchainDiagnostic CreateDiagnostic(string code, string message)
    {
        return new ToolchainDiagnostic(
            code,
            ToolchainDiagnosticSeverity.Error,
            message,
            null,
            []);
    }
}
