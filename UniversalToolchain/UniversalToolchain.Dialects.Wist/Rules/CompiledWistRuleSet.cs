using ExceptionsManager;
using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.Rules.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Rules;

public sealed class CompiledWistRuleSet : ICompiledRuleSet
{
    private readonly IReadOnlyDictionary<string, ICompiledRule> _rulesByName;

    public CompiledWistRuleSet(IReadOnlyList<ICompiledRule> rules)
    {
        rules = rules.ArgNotNull();
        _rulesByName = rules
            .OrderBy(static x => x.Descriptor.Name, StringComparer.Ordinal)
            .ToDictionary(static x => x.Descriptor.Name, static x => x, StringComparer.Ordinal);
        Rules = _rulesByName.Values
            .Select(static x => x.Descriptor)
            .ToList();
    }

    public IReadOnlyList<CompiledRuleDescriptor> Rules { get; }

    public object? Run(string ruleName, IReadOnlyDictionary<string, object?> arguments)
    {
        var result = TryRun(ruleName, arguments);
        if (!result.IsSuccess)
        {
            var message = string.Join(Environment.NewLine, result.Diagnostics.Select(static x => x.Message));
            Thrower.Argument(nameof(ruleName), message);
        }

        return result.Value;
    }

    public RuleExecutionResult TryRun(string ruleName, IReadOnlyDictionary<string, object?> arguments)
    {
        ruleName = ruleName.ArgNotNull();
        arguments = arguments.ArgNotNull();

        if (!_rulesByName.TryGetValue(ruleName, out var rule))
        {
            return RuleExecutionResult.Failure(
            [
                new ToolchainDiagnostic(
                    ToolchainDiagnosticCodes.RuleUnknown,
                    ToolchainDiagnosticSeverity.Error,
                    $"Unknown rule '{ruleName}'.",
                    null,
                    [])
            ]);
        }

        return rule.TryRun(arguments);
    }

    public RuleSetSchema GetSchema()
    {
        return new RuleSetSchema(
            Rules.Select(static x => new RuleSchema(
                    x.Name,
                    x.Parameters.Select(static y => new RuleParameterSchema(y.Name, y.Type.Name)).ToList(),
                    x.ReturnType.Name))
                .ToList());
    }
}
