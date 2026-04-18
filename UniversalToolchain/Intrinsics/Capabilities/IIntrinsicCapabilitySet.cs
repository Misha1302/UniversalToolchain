namespace BasicCore.Capabilities;

public interface IIntrinsicCapabilitySet
{
    bool Supports(IntrinsicSymbol symbol, IReadOnlyList<IntrinsicTypeArgument> typeArguments);
}