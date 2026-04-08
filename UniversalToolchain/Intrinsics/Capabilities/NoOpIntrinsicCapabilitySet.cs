using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Capabilities;

public sealed class NoOpIntrinsicCapabilitySet : IIntrinsicCapabilitySet
{
    public bool Supports(IntrinsicSymbol symbol, IReadOnlyList<IntrinsicTypeArgument> typeArguments)
    {
        return false;
    }
}
