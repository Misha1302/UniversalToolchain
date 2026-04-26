using BasicCore.Compilation;
using ExceptionsManager;
using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.Rules.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Rules;

public sealed class CompiledWistRule : ICompiledRule
{
    private readonly ICompiledArtifact _artifact;
    private readonly WistRuleRuntimeValueAdapter _runtimeValueAdapter = new();

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

        var conversion = ConvertArguments(arguments);
        if (conversion.Diagnostics.Count > 0)
            return RuleExecutionResult.Failure(conversion.Diagnostics);

        var session = _artifact.CreateSession();
        foreach (var parameter in Descriptor.Parameters)
            session.SetArgument(parameter.Name, conversion.RuntimeArguments[parameter.Name]);

        return RuleExecutionResult.Success(session.Run());
    }

    private RuleArgumentConversionResult ConvertArguments(IReadOnlyDictionary<string, object?> arguments)
    {
        var diagnostics = new List<ToolchainDiagnostic>();
        var runtimeArguments = new Dictionary<string, object?>(StringComparer.Ordinal);
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

            if (!_runtimeValueAdapter.TryConvert(
                    parameter.Type,
                    arguments[parameter.Name],
                    out var runtimeValue,
                    out var diagnostic,
                    parameter.Name,
                    Descriptor.Name))
            {
                diagnostics.Add(diagnostic.NotNull());
                continue;
            }

            runtimeArguments[parameter.Name] = runtimeValue;
        }

        return new RuleArgumentConversionResult(runtimeArguments, diagnostics);
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

    private sealed record RuleArgumentConversionResult(
        IReadOnlyDictionary<string, object?> RuntimeArguments,
        IReadOnlyList<ToolchainDiagnostic> Diagnostics);
}
