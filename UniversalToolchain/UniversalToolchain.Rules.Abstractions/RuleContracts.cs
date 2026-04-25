using UniversalToolchain.Diagnostics.Abstractions;

namespace UniversalToolchain.Rules.Abstractions;

public sealed record RuleTypeDescriptor(string Name);

public sealed record RuleParameterDescriptor(
    string Name,
    RuleTypeDescriptor Type);

public sealed record CompiledRuleDescriptor(
    string Name,
    IReadOnlyList<RuleParameterDescriptor> Parameters,
    RuleTypeDescriptor ReturnType);

public sealed record RuleExecutionResult(
    bool IsSuccess,
    object? Value,
    IReadOnlyList<ToolchainDiagnostic> Diagnostics)
{
    public static RuleExecutionResult Success(object? value)
    {
        return new RuleExecutionResult(true, value, []);
    }

    public static RuleExecutionResult Failure(IReadOnlyList<ToolchainDiagnostic> diagnostics)
    {
        return new RuleExecutionResult(false, null, diagnostics);
    }
}

public interface ICompiledRule
{
    CompiledRuleDescriptor Descriptor { get; }

    object? Run(IReadOnlyDictionary<string, object?> arguments);

    RuleExecutionResult TryRun(IReadOnlyDictionary<string, object?> arguments);
}

public interface ICompiledRuleSet
{
    IReadOnlyList<CompiledRuleDescriptor> Rules { get; }

    object? Run(string ruleName, IReadOnlyDictionary<string, object?> arguments);

    RuleExecutionResult TryRun(string ruleName, IReadOnlyDictionary<string, object?> arguments);

    RuleSetSchema GetSchema();
}

public sealed record RuleSetCompileResult(
    bool IsSuccess,
    ICompiledRuleSet? RuleSet,
    IReadOnlyList<ToolchainDiagnostic> Diagnostics)
{
    public static RuleSetCompileResult Success(ICompiledRuleSet ruleSet)
    {
        return new RuleSetCompileResult(true, ruleSet, []);
    }

    public static RuleSetCompileResult Failure(IReadOnlyList<ToolchainDiagnostic> diagnostics)
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
