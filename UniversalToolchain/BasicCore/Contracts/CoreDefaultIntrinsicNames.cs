using BasicCore.Capabilities;

namespace BasicCore.Contracts;

internal static class CoreDefaultIntrinsicNames
{
    public static IReadOnlyList<string> Value { get; } =
    [
        Encode(BuiltinIntrinsicSymbols.Core.CallCSharp),
        Encode(BuiltinIntrinsicSymbols.Core.CallCSharpCtor)
    ];

    private static string Encode(IntrinsicSymbol symbol)
    {
        if (IntrinsicCapabilityNameEncoder.TryEncode(symbol, [], out var name))
            return name;

        Thrower.InvalidOpEx($"Core intrinsic '{symbol}' cannot be encoded as a compiler capability identifier.");
        return string.Empty;
    }
}
