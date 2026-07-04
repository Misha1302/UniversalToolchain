using System.Collections.ObjectModel;

namespace UniversalToolchain.Ir.Abstractions;

/// <summary>
/// Immutable capability snapshot used by planners and legality checks.
/// </summary>
public sealed class CapabilitySet
{
    private readonly ReadOnlyCollection<CapabilityId> _capabilities;
    private readonly HashSet<CapabilityId> _lookup;

    public CapabilitySet(IEnumerable<CapabilityId>? capabilities = null)
    {
        var orderedCapabilities = (capabilities ?? [])
            .Distinct()
            .Order()
            .ToList();

        _capabilities = new ReadOnlyCollection<CapabilityId>(orderedCapabilities);
        _lookup = orderedCapabilities.ToHashSet();
    }

    public static CapabilitySet Empty { get; } = new();

    public IReadOnlyList<CapabilityId> Values => _capabilities;

    public bool Supports(CapabilityId capability) => _lookup.Contains(capability);
}
