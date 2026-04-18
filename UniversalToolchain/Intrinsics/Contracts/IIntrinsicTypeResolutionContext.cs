namespace BasicCore.Contracts;

public interface IIntrinsicTypeResolutionContext
{
    Type Resolve(IntrinsicTypeArgument argument);

    bool AreCompatible(Type expected, Type actual);
}