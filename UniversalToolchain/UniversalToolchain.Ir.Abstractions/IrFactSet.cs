using System.Collections.ObjectModel;

namespace UniversalToolchain.Ir.Abstractions;

/// <summary>
/// Immutable analysis fact snapshot available at a pipeline boundary.
/// </summary>
public sealed class IrFactSet
{
    private readonly ReadOnlyCollection<FactId> _facts;
    private readonly HashSet<FactId> _lookup;

    public IrFactSet(IEnumerable<FactId>? facts = null)
    {
        var orderedFacts = (facts ?? [])
            .Distinct()
            .Order()
            .ToList();

        _facts = new ReadOnlyCollection<FactId>(orderedFacts);
        _lookup = orderedFacts.ToHashSet();
    }

    public static IrFactSet Empty { get; } = new();

    public IReadOnlyList<FactId> Values => _facts;

    public bool Contains(FactId fact) => _lookup.Contains(fact);
}
