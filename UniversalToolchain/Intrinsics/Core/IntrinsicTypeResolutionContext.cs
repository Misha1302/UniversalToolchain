using DotnetHelper;

namespace BasicCore.Core;

public sealed class IntrinsicTypeResolutionContext : IIntrinsicTypeResolutionContext
{
    public Type Resolve(IntrinsicTypeArgument argument) => argument.RuntimeType;

    public bool AreCompatible(Type expected, Type actual)
    {
        if (expected == actual)
            return true;

        return expected.IsAssignableFrom(actual) ||
               UserDefinedConversionResolver.CanConvert(actual, expected);
    }
}