namespace BasicCore.Capabilities;

public sealed class NoOpIntrinsicCapabilitySet : IIntrinsicCapabilitySet
{
    public bool Supports(IntrinsicSymbol symbol, IReadOnlyList<IntrinsicTypeArgument> typeArguments) => false;
}