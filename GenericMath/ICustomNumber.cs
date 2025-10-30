// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using EqualityModule;

namespace GenericMath;

public interface ICustomNumber<TSelf, TValue>
    : IAddable<TSelf>, IMulAble<TSelf>, ISubAble<TSelf>, IDivisible<TSelf>, IGettable<TValue>
    where TSelf : ICustomNumber<TSelf, TValue>
{
    public static abstract TSelf Create(TValue value);
}