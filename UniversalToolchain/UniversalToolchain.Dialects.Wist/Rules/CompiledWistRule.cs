using BasicCore.Compilation;
using ExceptionsManager;
using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.Rules.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Rules;

public sealed class CompiledWistRule : ICompiledRule
{
    private readonly ICompiledArtifact _artifact;

    public CompiledWistRule(CompiledRuleDescriptor descriptor, ICompiledArtifact artifact)
    {
        Descriptor = descriptor.ArgNotNull();
        _artifact = artifact.ArgNotNull();
    }

    public CompiledRuleDescriptor Descriptor { get; }

    public object? Run(IReadOnlyDictionary<string, object?> arguments)
    {
        var result = TryRun(arguments);
        if (!result.IsSuccess)
        {
            var message = string.Join(Environment.NewLine, result.Diagnostics.Select(static x => x.Message));
            Thrower.Argument(nameof(arguments), message);
        }

        return result.Value;
    }

    public RuleExecutionResult TryRun(IReadOnlyDictionary<string, object?> arguments)
    {
        arguments = arguments.ArgNotNull();

        var diagnostics = ValidateArguments(arguments);
        if (diagnostics.Count > 0)
            return RuleExecutionResult.Failure(diagnostics);

        var session = _artifact.CreateSession();
        foreach (var parameter in Descriptor.Parameters)
            session.SetArgument(parameter.Name, arguments[parameter.Name]);

        return RuleExecutionResult.Success(session.Run());
    }

    private IReadOnlyList<ToolchainDiagnostic> ValidateArguments(IReadOnlyDictionary<string, object?> arguments)
    {
        var diagnostics = new List<ToolchainDiagnostic>();
        var requiredNames = Descriptor.Parameters
            .Select(static x => x.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var argument in arguments.Keys.OrderBy(static x => x, StringComparer.Ordinal))
        {
            if (!requiredNames.Contains(argument))
            {
                diagnostics.Add(CreateDiagnostic(
                    ToolchainDiagnosticCodes.RuleArgumentUnknown,
                    $"Unknown argument '{argument}' for rule '{Descriptor.Name}'."));
            }
        }

        foreach (var parameter in Descriptor.Parameters)
        {
            if (!arguments.ContainsKey(parameter.Name))
            {
                diagnostics.Add(CreateDiagnostic(
                    ToolchainDiagnosticCodes.RuleArgumentMissing,
                    $"Missing required argument '{parameter.Name}' for rule '{Descriptor.Name}'."));
                continue;
            }

            var value = arguments[parameter.Name];
            if (value == null)
            {
                diagnostics.Add(CreateDiagnostic(
                    ToolchainDiagnosticCodes.RuleArgumentNull,
                    $"Argument '{parameter.Name}' for rule '{Descriptor.Name}' must not be null."));
                continue;
            }

            if (!IsRuntimeValueCompatible(parameter.Type, value))
            {
                diagnostics.Add(CreateDiagnostic(
                    ToolchainDiagnosticCodes.RuleArgumentTypeMismatch,
                    $"Argument '{parameter.Name}' for rule '{Descriptor.Name}' must have type '{parameter.Type.Name}'. Actual runtime type: '{value.GetType().FullName}'."));
            }
        }

        return diagnostics;
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

    private static bool IsRuntimeValueCompatible(RuleTypeDescriptor type, object value)
    {
        return type.Name switch
        {
            "number" => value is double or float or decimal or int or long or short or byte,
            "bool" => value is bool,
            _ => false
        };
    }
}
