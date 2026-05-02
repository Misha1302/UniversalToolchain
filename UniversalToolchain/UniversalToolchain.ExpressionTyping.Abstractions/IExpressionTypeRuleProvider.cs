namespace UniversalToolchain.ExpressionTyping.Abstractions;

public interface IExpressionTypeRuleProvider
{
    IReadOnlyList<IExpressionTypeRule> GetRules();
}