using BasicCore.Compilation;
using ExceptionsManager;
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
        arguments = arguments.ArgNotNull();
        ValidateArguments(arguments);

        var session = _artifact.CreateSession();
        foreach (var parameter in Descriptor.Parameters)
            session.SetArgument(parameter.Name, arguments[parameter.Name]);

        return session.Run();
    }

    private void ValidateArguments(IReadOnlyDictionary<string, object?> arguments)
    {
        var requiredNames = Descriptor.Parameters
            .Select(static x => x.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var argument in arguments.Keys)
        {
            if (!requiredNames.Contains(argument))
                Thrower.Argument(nameof(arguments), $"Unknown argument '{argument}' for rule '{Descriptor.Name}'.");
        }

        foreach (var parameter in Descriptor.Parameters)
        {
            if (!arguments.ContainsKey(parameter.Name))
                Thrower.Argument(nameof(arguments), $"Missing required argument '{parameter.Name}' for rule '{Descriptor.Name}'.");

            var value = arguments[parameter.Name];
            if (value == null)
                Thrower.Argument(nameof(arguments), $"Argument '{parameter.Name}' for rule '{Descriptor.Name}' must not be null.");

            if (!IsRuntimeValueCompatible(parameter.Type, value))
            {
                Thrower.Argument(
                    nameof(arguments),
                    $"Argument '{parameter.Name}' for rule '{Descriptor.Name}' must have type '{parameter.Type.Name}'. Actual runtime type: '{value.GetType().FullName}'.");
            }
        }
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
