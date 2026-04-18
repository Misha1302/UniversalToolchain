namespace BasicCore.Capabilities;

public sealed class OptimizerIntrinsicCapabilityContext : IOptimizerIntrinsicCapabilityContext
{
    private readonly IIntrinsicCapabilitySet _capabilitySet;

    public OptimizerIntrinsicCapabilityContext(IIntrinsicCapabilitySet capabilitySet)
    {
        capabilitySet = capabilitySet.ArgNotNull();

        _capabilitySet = capabilitySet;
    }

    public bool Supports(IntrinsicSymbol symbol, params Type[] typeArguments) => Supports(symbol, (IReadOnlyList<Type>)typeArguments);

    public bool Supports(IntrinsicSymbol symbol, IReadOnlyList<Type> typeArguments)
    {
        typeArguments = typeArguments.ArgNotNull();

        return _capabilitySet.Supports(symbol, typeArguments.Select(IntrinsicTypeArgument.From).ToArray());
    }
}