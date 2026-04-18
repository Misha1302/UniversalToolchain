namespace BasicCore.Capabilities;

public interface IOptimizerIntrinsicCapabilityContext
{
    bool Supports(IntrinsicSymbol symbol, params Type[] typeArguments);

    bool Supports(IntrinsicSymbol symbol, IReadOnlyList<Type> typeArguments);
}