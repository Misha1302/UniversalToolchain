// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

namespace GenericMath;

public interface IBinaryOperation<TSelf> where TSelf : IBinaryOperation<TSelf>
{
    // x * y = y * x
    public static virtual bool IsCommutative()
    {
        return false;
    }

    // (a * b) * c = a * (b * c)
    public static virtual bool IsAssociative()
    {
        return false;
    }

    // a(b + c) = ab + ac
    public static virtual bool IsDistributive<TOther>() where TOther : IBinaryOperation<TOther>
    {
        return false;
    }
}