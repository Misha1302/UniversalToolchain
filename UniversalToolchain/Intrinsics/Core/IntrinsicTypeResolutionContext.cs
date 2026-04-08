using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Core;

public sealed class IntrinsicTypeResolutionContext : IIntrinsicTypeResolutionContext
{
    public Type Resolve(IntrinsicTypeArgument argument)
    {
        return argument.RuntimeType;
    }

    public bool AreCompatible(Type expected, Type actual)
    {
        if (expected == actual)
            return true;

        return expected.IsAssignableFrom(actual);
    }
}
