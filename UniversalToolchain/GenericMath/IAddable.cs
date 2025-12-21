namespace GenericMath;

public interface IAddable<TSelf> : IBinaryOperation<TSelf> where TSelf : IAddable<TSelf>
{
    public static abstract TSelf Add(TSelf a, TSelf b);
}