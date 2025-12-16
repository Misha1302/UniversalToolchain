// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

namespace GenericMath;

public interface IAddable<TSelf> : IBinaryOperation<TSelf> where TSelf : IAddable<TSelf>
{
    public static abstract TSelf Add(TSelf a, TSelf b);
}