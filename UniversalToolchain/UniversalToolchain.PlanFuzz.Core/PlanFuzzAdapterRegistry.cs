namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Stores explicitly registered language adapters without embedding language-specific selection logic in the core.
/// </summary>
public sealed class PlanFuzzAdapterRegistry
{
    private readonly Dictionary<string, IPlanFuzzLanguageAdapter> _adapters = new(StringComparer.Ordinal);

    public PlanFuzzAdapterRegistry Add(IPlanFuzzLanguageAdapter adapter)
    {
        adapter = adapter.ArgNotNull();
        if (!_adapters.TryAdd(adapter.Descriptor.AdapterId, adapter))
            Thrower.InvalidOpEx($"PlanFuzz adapter '{adapter.Descriptor.AdapterId}' is already registered.");
        return this;
    }

    public IPlanFuzzLanguageAdapter GetRequired(string adapterId)
    {
        if (string.IsNullOrWhiteSpace(adapterId))
            return Thrower.Argument<IPlanFuzzLanguageAdapter>(nameof(adapterId), "Adapter ID must not be empty.");
        return _adapters.TryGetValue(adapterId, out var adapter)
            ? adapter
            : Thrower.InvalidOpEx<IPlanFuzzLanguageAdapter>($"PlanFuzz adapter '{adapterId}' is not registered.");
    }

    public IReadOnlyList<PlanFuzzAdapterDescriptor> Descriptors =>
        _adapters.Values.Select(static adapter => adapter.Descriptor)
            .OrderBy(static descriptor => descriptor.AdapterId, StringComparer.Ordinal)
            .ToArray();
}
