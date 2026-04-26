using ExceptionsManager;
using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.Rules.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Rules;

public interface IWistRuleArgumentBinder
{
    RuleArgumentBindingResult Bind(CompiledRuleDescriptor descriptor, IReadOnlyDictionary<string, object?> arguments);
}

public sealed record RuleArgumentBindingResult(
    bool IsSuccess,
    IReadOnlyDictionary<string, object?> RuntimeArguments,
    IReadOnlyList<ToolchainDiagnostic> Diagnostics)
{
    public static RuleArgumentBindingResult Success(IReadOnlyDictionary<string, object?> runtimeArguments)
    {
        return new RuleArgumentBindingResult(true, runtimeArguments, []);
    }

    public static RuleArgumentBindingResult Failure(IReadOnlyList<ToolchainDiagnostic> diagnostics)
    {
        return new RuleArgumentBindingResult(false, new Dictionary<string, object?>(StringComparer.Ordinal), diagnostics);
    }
}

public sealed class WistRuleArgumentBinder : IWistRuleArgumentBinder
{
    private readonly WistRuleRuntimeValueAdapter _runtimeValueAdapter = new();

    public RuleArgumentBindingResult Bind(CompiledRuleDescriptor descriptor, IReadOnlyDictionary<string, object?> arguments)
    {
        descriptor = descriptor.ArgNotNull();
        arguments = arguments.ArgNotNull();

        var diagnostics = new List<ToolchainDiagnostic>();
        var runtimeArguments = new Dictionary<string, object?>(StringComparer.Ordinal);
        var requiredNames = descriptor.Parameters
            .Select(static x => x.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var argument in arguments.Keys.OrderBy(static x => x, StringComparer.Ordinal))
        {
            if (!requiredNames.Contains(argument))
                diagnostics.Add(CreateDiagnostic(ToolchainDiagnosticCodes.RuleArgumentUnknown, $"Unknown argument '{argument}' for rule '{descriptor.Name}'."));
        }

        foreach (var parameter in descriptor.Parameters)
        {
            if (!arguments.ContainsKey(parameter.Name))
            {
                diagnostics.Add(CreateDiagnostic(ToolchainDiagnosticCodes.RuleArgumentMissing, $"Missing required argument '{parameter.Name}' for rule '{descriptor.Name}'."));
                continue;
            }

            if (!_runtimeValueAdapter.TryConvert(parameter.Type, arguments[parameter.Name], out var runtimeValue, out var diagnostic, parameter.Name, descriptor.Name))
            {
                diagnostics.Add(diagnostic.NotNull());
                continue;
            }

            runtimeArguments[parameter.Name] = runtimeValue;
        }

        return diagnostics.Count == 0
            ? RuleArgumentBindingResult.Success(runtimeArguments)
            : RuleArgumentBindingResult.Failure(diagnostics);
    }

    private static ToolchainDiagnostic CreateDiagnostic(string code, string message)
    {
        return new ToolchainDiagnostic(code, ToolchainDiagnosticSeverity.Error, message, null, []);
    }
}
