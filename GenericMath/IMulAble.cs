// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

namespace GenericMath;

public interface IMulAble<TSelf> : IBinaryOperation<TSelf> where TSelf : IMulAble<TSelf>
{
    public static abstract TSelf Mul(TSelf a, TSelf b);
}