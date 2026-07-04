namespace UniversalToolchain.Ir.Abstractions;

/// <summary>
/// Declares stage legality so planners and verifiers do not rely on implicit pass ordering.
/// </summary>
public sealed class IrStageContract
{
    public IrStageContract(
        IEnumerable<FactId>? requiresFacts = null,
        IEnumerable<FactId>? producesFacts = null,
        IEnumerable<FactId>? preservesFacts = null,
        IEnumerable<FactId>? invalidatesFacts = null,
        IEnumerable<CapabilityId>? requiresCapabilities = null)
    {
        RequiresFacts = new IrFactSet(requiresFacts);
        ProducesFacts = new IrFactSet(producesFacts);
        PreservesFacts = new IrFactSet(preservesFacts);
        InvalidatesFacts = new IrFactSet(invalidatesFacts);
        RequiresCapabilities = new CapabilitySet(requiresCapabilities);
    }

    public static IrStageContract Empty { get; } = new();

    public IrFactSet RequiresFacts { get; }

    public IrFactSet ProducesFacts { get; }

    public IrFactSet PreservesFacts { get; }

    public IrFactSet InvalidatesFacts { get; }

    public CapabilitySet RequiresCapabilities { get; }
}
