using ExceptionsManager;
using NumbersModule.Core;
using UniversalToolchain.Rules.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Rules;

public sealed class WistRuleRuntimeTypeResolver
{
    private static readonly IReadOnlyDictionary<string, Type> _runtimeTypes = new Dictionary<string, Type>(StringComparer.Ordinal)
    {
        ["number"] = typeof(RealNumberImpl),
        ["bool"] = typeof(bool)
    };

    public bool TryResolve(RuleTypeDescriptor type, out Type runtimeType)
    {
        type = type.ArgNotNull();
        return _runtimeTypes.TryGetValue(type.Name, out runtimeType!);
    }
}
