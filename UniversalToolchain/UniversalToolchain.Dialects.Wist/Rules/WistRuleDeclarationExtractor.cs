using ExceptionsManager;
using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.Dialects.Wist.Rules.Syntax;
using UniversalToolchain.Rules.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Rules;

public sealed class WistRuleDeclarationExtractor
{
    private readonly WistRuleBodySyntaxAnalyzer _bodySyntaxAnalyzer;
    private readonly WistRuleSetSyntaxParser _syntaxParser;

    public WistRuleDeclarationExtractor()
    {
        _syntaxParser = new WistRuleSetSyntaxParser();
        _bodySyntaxAnalyzer = new WistRuleBodySyntaxAnalyzer();
    }

    public RuleDeclarationExtractionResult Extract(string source)
    {
        source = source.ArgNotNull();

        var parse = _syntaxParser.Parse(source);
        if (!parse.IsSuccess || parse.Syntax == null)
            return RuleDeclarationExtractionResult.Failure(parse.Diagnostics);

        var diagnostics = new List<ToolchainDiagnostic>();
        var rules = new List<RuleDeclarationModel>();
        var ruleNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var ruleSyntax in parse.Syntax.Rules)
        {
            if (!ruleNames.Add(ruleSyntax.Name))
                diagnostics.Add(CreateDiagnostic(ToolchainDiagnosticCodes.RuleDuplicateName, $"Duplicate rule declaration '{ruleSyntax.Name}'."));

            var returnType = ParseType(ruleSyntax.ReturnType.Name, diagnostics, $"rule '{ruleSyntax.Name}' return type");
            if (returnType == null)
                continue;

            var parameters = ParseParameters(ruleSyntax, diagnostics);
            var bodyInfo = _bodySyntaxAnalyzer.Analyze(ruleSyntax.BodySourceText, ruleSyntax.BodySpan.Start);
            diagnostics.AddRange(bodyInfo.Diagnostics);

            rules.Add(new RuleDeclarationModel(
                ruleSyntax.Name,
                parameters,
                returnType,
                new RuleBodyModel(ruleSyntax.BodySourceText, bodyInfo.LocalBindings, ruleSyntax.BodySpan),
                ruleSyntax.Span));
        }

        return diagnostics.Count == 0
            ? RuleDeclarationExtractionResult.Success(rules)
            : new RuleDeclarationExtractionResult(false, rules, diagnostics);
    }

    private static IReadOnlyList<RuleParameterModel> ParseParameters(WistRuleDeclarationSyntax syntax, List<ToolchainDiagnostic> diagnostics)
    {
        var parameters = new List<RuleParameterModel>();

        var parameterNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var parameterSyntax in syntax.Parameters)
        {
            if (!parameterNames.Add(parameterSyntax.Name))
            {
                diagnostics.Add(CreateDiagnostic(ToolchainDiagnosticCodes.RuleDuplicateParameter, $"Duplicate parameter '{parameterSyntax.Name}' in rule '{syntax.Name}'."));
                continue;
            }

            var type = ParseType(parameterSyntax.Type.Name, diagnostics, $"parameter '{parameterSyntax.Name}' in rule '{syntax.Name}'");
            if (type != null)
                parameters.Add(new RuleParameterModel(parameterSyntax.Name, type));
        }

        return parameters;
    }

    private static RuleTypeDescriptor? ParseType(string typeName, List<ToolchainDiagnostic> diagnostics, string owner)
    {
        if (string.Equals(typeName, "number", StringComparison.Ordinal) || string.Equals(typeName, "bool", StringComparison.Ordinal))
            return new RuleTypeDescriptor(typeName);

        diagnostics.Add(CreateDiagnostic(ToolchainDiagnosticCodes.RuleUnknownType, $"Unsupported type '{typeName}' for {owner}. Supported MVP types: number, bool."));
        return null;
    }

    private static ToolchainDiagnostic CreateDiagnostic(string code, string message)
    {
        return new ToolchainDiagnostic(code, ToolchainDiagnosticSeverity.Error, message, null, []);
    }
}
