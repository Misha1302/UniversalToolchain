using System.Collections.ObjectModel;
using UniversalToolchain.Semantics.Abstractions;

namespace UniversalToolchain.Ssa.Abstractions;

/// <summary>
/// Descriptor-driven SSA call instruction. This is the long-term semantic operation
/// shape: arithmetic, runtime calls, constructors and intrinsics should be modeled
/// as callables rather than built into the SSA core.
/// </summary>
public sealed class SsaCall : ISsaInstruction
{
    public SsaCall(
        SsaOperationId id,
        CallableId callee,
        IEnumerable<SsaValueId>? arguments = null,
        IEnumerable<SsaValue>? results = null,
        SsaAttributeBag? attributes = null)
    {
        Id = id;
        Callee = callee;
        Arguments = new ReadOnlyCollection<SsaValueId>((arguments ?? []).ToList());
        Results = new ReadOnlyCollection<SsaValue>((results ?? []).ToList());
        Attributes = attributes ?? SsaAttributeBag.Empty;
    }

    public SsaOperationId Id { get; }

    public CallableId Callee { get; }

    public IReadOnlyList<SsaValueId> Arguments { get; }

    public IReadOnlyList<SsaValueId> Operands => Arguments;

    public IReadOnlyList<SsaValue> Results { get; }

    public SsaAttributeBag Attributes { get; }
}
