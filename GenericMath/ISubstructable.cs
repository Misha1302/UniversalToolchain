// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

namespace GenericMath;

public interface ISubstructable<TSelf> : IBinaryOperation<TSelf> where TSelf : IMulable<TSelf>
{
    static bool IBinaryOperation<TSelf>.IsDistributive<TOther>()
    {
        return typeof(TOther).IsAssignableFrom(typeof(IMulable<>).MakeGenericType(typeof(TSelf)));
    }

    public static abstract TSelf Sub(TSelf a, TSelf b);
}