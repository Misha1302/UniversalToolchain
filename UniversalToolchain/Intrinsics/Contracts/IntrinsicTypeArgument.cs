namespace UniversalToolchain.Intrinsics.Contracts;

public readonly record struct IntrinsicTypeArgument(Type RuntimeType)
{
    public static IntrinsicTypeArgument From(Type type)
    {
        type = type.ArgNotNull();

        return new IntrinsicTypeArgument(type);
    }

    public override string ToString()
    {
        return RuntimeType.FullName ?? RuntimeType.Name;
    }
}
