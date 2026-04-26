using ExceptionsManager;
using UniversalToolchain.Dialects.Wist.Rules.Syntax;
using UniversalToolchain.Rules.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Rules;

public sealed class WistRuleDeclarationExtractor
{
    private readonly WistRuleSetSyntaxParser _syntaxParser;

    public WistRuleDeclarationExtractor()
    {
        _syntaxParser = new WistRuleSetSyntaxParser();
    }

    public RuleDeclarationExtractionResult Extract(string source)
    {
        source = source.ArgNotNull();

        var parse = _syntaxParser.Parse(source);
        if (!parse.IsSuccess || parse.Syntax == null)
            return RuleDeclarationExtractionResult.Failure(parse.Diagnostics);

        var rules = new List<RuleDeclarationModel>();

        foreach (var ruleSyntax in parse.Syntax.Rules)
        {
            var parameters = ParseParameters(ruleSyntax);

            rules.Add(new RuleDeclarationModel(
                ruleSyntax.Name,
                parameters,
                new RuleTypeDescriptor(ruleSyntax.ReturnType.Name),
                new RuleBodyModel(ruleSyntax.BodySourceText, [], ruleSyntax.BodySpan),
                ruleSyntax.Span));
        }

        // Rule-local binding validation requires AST-backed body extraction.
        // Raw body-source scanning is intentionally not used as a temporary substitute.
        return RuleDeclarationExtractionResult.Success(rules);
    }

    private static IReadOnlyList<RuleParameterModel> ParseParameters(WistRuleDeclarationSyntax syntax)
    {
        var parameters = new List<RuleParameterModel>();

        foreach (var parameterSyntax in syntax.Parameters)
            parameters.Add(new RuleParameterModel(parameterSyntax.Name, new RuleTypeDescriptor(parameterSyntax.Type.Name)));

        return parameters;
    }
}
