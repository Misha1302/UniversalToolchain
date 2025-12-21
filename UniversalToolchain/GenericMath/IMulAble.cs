namespace GenericMath;

public interface IMulAble<TSelf> : IBinaryOperation<TSelf> where TSelf : IMulAble<TSelf>
{
    public static abstract TSelf Mul(TSelf a, TSelf b);
}