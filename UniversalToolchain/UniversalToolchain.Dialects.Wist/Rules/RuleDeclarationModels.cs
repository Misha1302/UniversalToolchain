using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.Rules.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Rules;

public readonly record struct RuleScopeId(int Value);

public readonly record struct SourceSpan(int Start, int Length);

public sealed record LocalBindingDeclarationModel(
    string Name,
    int DeclarationOrder,
    RuleScopeId ScopeId,
    SourceSpan Span);

public sealed record RuleBodyModel(
    string SourceText,
    IReadOnlyList<LocalBindingDeclarationModel> LocalBindings,
    SourceSpan Span);

public sealed record RuleDeclarationModel(
    string Name,
    IReadOnlyList<RuleParameterModel> Parameters,
    RuleTypeDescriptor ReturnType,
    RuleBodyModel Body,
    SourceSpan Span);

public sealed record RuleParameterModel(
    string Name,
    RuleTypeDescriptor Type);

public sealed record RuleDeclarationExtractionResult(
    bool IsSuccess,
    IReadOnlyList<RuleDeclarationModel> Rules,
    IReadOnlyList<ToolchainDiagnostic> Diagnostics)
{
    public static RuleDeclarationExtractionResult Success(IReadOnlyList<RuleDeclarationModel> rules)
    {
        return new RuleDeclarationExtractionResult(true, rules, []);
    }

    public static RuleDeclarationExtractionResult Failure(IReadOnlyList<ToolchainDiagnostic> diagnostics)
    {
        return new RuleDeclarationExtractionResult(false, [], diagnostics);
    }
}
