namespace UniversalToolchain.Rules.Abstractions;

public sealed record RuleTypeDescriptor(string Name);

public sealed record RuleParameterDescriptor(
    string Name,
    RuleTypeDescriptor Type);

public sealed record CompiledRuleDescriptor(
    string Name,
    IReadOnlyList<RuleParameterDescriptor> Parameters,
    RuleTypeDescriptor ReturnType);

public interface ICompiledRule
{
    CompiledRuleDescriptor Descriptor { get; }

    object? Run(IReadOnlyDictionary<string, object?> arguments);
}

public interface ICompiledRuleSet
{
    IReadOnlyList<CompiledRuleDescriptor> Rules { get; }

    object? Run(string ruleName, IReadOnlyDictionary<string, object?> arguments);

    RuleSetSchema GetSchema();
}

public sealed record RuleSetCompileResult(
    bool IsSuccess,
    ICompiledRuleSet? RuleSet,
    IReadOnlyList<string> Diagnostics)
{
    public static RuleSetCompileResult Success(ICompiledRuleSet ruleSet)
    {
        return new RuleSetCompileResult(true, ruleSet, []);
    }

    public static RuleSetCompileResult Failure(IReadOnlyList<string> diagnostics)
    {
        return new RuleSetCompileResult(false, null, diagnostics);
    }
}

public sealed record RuleSetSchema(IReadOnlyList<RuleSchema> Rules);

public sealed record RuleSchema(
    string Name,
    IReadOnlyList<RuleParameterSchema> Parameters,
    string ReturnType);

public sealed record RuleParameterSchema(
    string Name,
    string Type);
