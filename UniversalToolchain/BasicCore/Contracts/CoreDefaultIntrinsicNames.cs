using BasicCore.Capabilities;
using BasicCore.Legacy;

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
        if (LegacyCapabilityNameEncoder.TryEncode(symbol, [], out var name))
            return name;

        Thrower.InvalidOpEx($"Core intrinsic '{symbol}' cannot be encoded as a legacy compiler capability name.");
        return string.Empty;
    }
}
