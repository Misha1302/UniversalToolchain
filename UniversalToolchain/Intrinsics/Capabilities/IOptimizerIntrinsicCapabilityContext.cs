using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Capabilities;

public interface IOptimizerIntrinsicCapabilityContext
{
    bool Supports(IntrinsicSymbol symbol, params Type[] typeArguments);

    bool Supports(IntrinsicSymbol symbol, IReadOnlyList<Type> typeArguments);
}