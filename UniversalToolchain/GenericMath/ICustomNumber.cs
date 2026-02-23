using SettableGettableModule;
using SettableGettableModule.Contracts;

namespace GenericMath;

public interface ICustomNumber<TSelf, TValue>
    : IAddable<TSelf>, IMulAble<TSelf>, ISubAble<TSelf>, IDivisible<TSelf>, IGettable<TValue>
    where TSelf : ICustomNumber<TSelf, TValue>
{
    public static abstract TSelf Create(TValue value);
}