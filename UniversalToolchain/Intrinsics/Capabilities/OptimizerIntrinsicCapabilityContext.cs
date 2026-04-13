using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Capabilities;

public sealed class OptimizerIntrinsicCapabilityContext : IOptimizerIntrinsicCapabilityContext
{
    private readonly IIntrinsicCapabilitySet _capabilitySet;

    public OptimizerIntrinsicCapabilityContext(IIntrinsicCapabilitySet capabilitySet)
    {
        if (capabilitySet == null)
            Thrower.ArgumentNull(nameof(capabilitySet));

        _capabilitySet = capabilitySet;
    }

    public bool Supports(IntrinsicSymbol symbol, params Type[] typeArguments)
    {
        return Supports(symbol, (IReadOnlyList<Type>)typeArguments);
    }

    public bool Supports(IntrinsicSymbol symbol, IReadOnlyList<Type> typeArguments)
    {
        if (typeArguments == null)
            Thrower.ArgumentNull(nameof(typeArguments));

        return _capabilitySet.Supports(symbol, typeArguments.Select(IntrinsicTypeArgument.From).ToArray());
    }
}
