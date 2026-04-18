using System.Collections.ObjectModel;

namespace BasicCore.Execution;

/// <summary>
///     Immutable name-to-slot layout for declared external bindings.
/// </summary>
public sealed class ExternalBindingsLayout
{
    private ExternalBindingsLayout(IReadOnlyDictionary<string, int> slotsByName)
    {
        slotsByName = slotsByName.ArgNotNull();

        SlotsByName = slotsByName;
    }

    /// <summary>
    ///     Gets external binding slots keyed by binding name.
    /// </summary>
    public IReadOnlyDictionary<string, int> SlotsByName { get; }

    /// <summary>
    ///     Builds immutable layout from declared bindings in their compile-time order.
    /// </summary>
    public static ExternalBindingsLayout FromDeclaredBindings(IReadOnlyList<ExternalBinding> declaredBindings)
    {
        declaredBindings = declaredBindings.ArgNotNull();

        var slots = new Dictionary<string, int>(declaredBindings.Count, StringComparer.Ordinal);
        for (var i = 0; i < declaredBindings.Count; i++)
        {
            var name = declaredBindings[i].Name;
            if (!slots.TryAdd(name, i))
                Thrower.Argument(nameof(declaredBindings), $"Declared binding '{name}' is duplicated.");
        }

        return new ExternalBindingsLayout(new ReadOnlyDictionary<string, int>(slots));
    }
}