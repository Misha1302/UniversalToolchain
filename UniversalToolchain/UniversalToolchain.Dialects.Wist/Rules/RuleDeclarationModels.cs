using UniversalToolchain.Rules.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Rules;

public sealed record RuleDeclarationModel(
    string Name,
    IReadOnlyList<RuleParameterModel> Parameters,
    RuleTypeDescriptor ReturnType,
    string Body);

public sealed record RuleParameterModel(
    string Name,
    RuleTypeDescriptor Type);

public sealed record RuleDeclarationExtractionResult(
    bool IsSuccess,
    IReadOnlyList<RuleDeclarationModel> Rules,
    IReadOnlyList<string> Diagnostics)
{
    public static RuleDeclarationExtractionResult Success(IReadOnlyList<RuleDeclarationModel> rules)
    {
        return new RuleDeclarationExtractionResult(true, rules, []);
    }

    public static RuleDeclarationExtractionResult Failure(IReadOnlyList<string> diagnostics)
    {
        return new RuleDeclarationExtractionResult(false, [], diagnostics);
    }
}
