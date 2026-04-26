using UniversalToolchain.Diagnostics.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Rules.Syntax;

public sealed record WistRuleSetSyntax(IReadOnlyList<WistRuleDeclarationSyntax> Rules);

public sealed record WistRuleDeclarationSyntax(
    string Name,
    IReadOnlyList<WistRuleParameterSyntax> Parameters,
    WistRuleTypeSyntax ReturnType,
    string BodySourceText,
    SourceSpan Span,
    SourceSpan BodySpan);

public sealed record WistRuleParameterSyntax(
    string Name,
    WistRuleTypeSyntax Type,
    SourceSpan Span);

public sealed record WistRuleTypeSyntax(
    string Name,
    SourceSpan Span);

public sealed record WistRuleSetSyntaxParseResult(
    bool IsSuccess,
    WistRuleSetSyntax? Syntax,
    IReadOnlyList<ToolchainDiagnostic> Diagnostics);

public sealed record WistRuleBodySyntaxInfo(
    IReadOnlyList<LocalBindingDeclarationModel> LocalBindings,
    IReadOnlyList<ToolchainDiagnostic> Diagnostics)
{
    public bool IsSuccess => Diagnostics.Count == 0;
}
