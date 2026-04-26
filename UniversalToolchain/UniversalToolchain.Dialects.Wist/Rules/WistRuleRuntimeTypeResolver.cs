using ExceptionsManager;
using UniversalToolchain.Rules.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Rules;

public sealed class WistRuleRuntimeTypeResolver
{
    private readonly IReadOnlyDictionary<string, RuleRuntimeTypeBinding> _bindings;

    public WistRuleRuntimeTypeResolver(IEnumerable<RuleRuntimeTypeBinding> bindings)
    {
        bindings = bindings.ArgNotNull();

        var orderedBindings = bindings
            .OrderBy(static x => x.RuleType.Name, StringComparer.Ordinal)
            .ThenBy(static x => x.RuntimeType.FullName ?? x.RuntimeType.Name, StringComparer.Ordinal)
            .ToList();

        var map = new Dictionary<string, RuleRuntimeTypeBinding>(StringComparer.Ordinal);
        foreach (var binding in orderedBindings)
        {
            if (!map.TryAdd(binding.RuleType.Name, binding))
                Thrower.InvalidOpEx($"Duplicate rule runtime type binding for rule type '{binding.RuleType.Name}'.");
        }

        _bindings = map;
    }

    public bool TryResolve(RuleTypeDescriptor type, out Type runtimeType)
    {
        type = type.ArgNotNull();

        if (TryGetBinding(type, out var binding))
        {
            runtimeType = binding.RuntimeType;
            return true;
        }

        runtimeType = null!;
        return false;
    }

    public bool TryGetBinding(RuleTypeDescriptor type, out RuleRuntimeTypeBinding binding)
    {
        type = type.ArgNotNull();
        return _bindings.TryGetValue(type.Name, out binding!);
    }
}
