namespace GenericMath;

public interface ISubAble<TSelf> : IBinaryOperation<TSelf> where TSelf : ISubAble<TSelf>
{
    public static abstract TSelf Sub(TSelf a, TSelf b);
}