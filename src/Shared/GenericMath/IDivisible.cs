namespace GenericMath;

public interface IDivisible<TSelf> : IBinaryOperation<TSelf> where TSelf : IDivisible<TSelf>
{
    public static abstract TSelf Div(TSelf a, TSelf b);
}