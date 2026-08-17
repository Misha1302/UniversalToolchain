using BasicCore.Binding;

namespace VariablesModule;

/// <summary>
/// Phase-specific semantic binding surface for runtimes that do not materialize the combined frontend module after syntax.
/// </summary>
public static class VariablesSemanticBindingProvider
{
    public static IReadOnlyList<IAstBindingRule> CreateRules() => [new VariablesBindingRule()];
}
