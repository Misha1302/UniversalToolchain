namespace UniversalToolchain.Air.Analysis;

/// <summary>
/// Structural AIR value source used while converting a backend-oriented external-slot load
/// into SSA. The marker is execution-local and must be lowered back to an AIR load before
/// reaching a backend.
/// </summary>
public sealed record AirExternalValueReference
{
    public AirExternalValueReference(int slot, Type valueType)
    {
        if (slot < 0)
            throw new ArgumentOutOfRangeException(nameof(slot), slot, "External value slot must not be negative.");

        Slot = slot;
        ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
    }

    public int Slot { get; }

    public Type ValueType { get; }
}
