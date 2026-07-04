namespace UniversalToolchain.Ir.Abstractions;

/// <summary>
/// Describes a generic intermediate layer constraint without naming a concrete implementation package.
/// </summary>
public sealed record IntermediateLayerRequest
{
    public IntermediateLayerRequest(IrKind irKind, IntermediateLayerPolicy policy)
    {
        if (!Enum.IsDefined(policy))
            throw new ArgumentOutOfRangeException(nameof(policy), policy, "Intermediate layer policy is not supported.");

        IrKind = irKind;
        Policy = policy;
    }

    public IrKind IrKind { get; }

    public IntermediateLayerPolicy Policy { get; }
}
