// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

namespace GenericMath;

public interface IDivisible<TSelf> : IBinaryOperation<TSelf> where TSelf : IMulable<TSelf>
{
    public static abstract TSelf Div(TSelf a, TSelf b);
}