// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

namespace GenericMath;

public interface IBinaryOperation<TSelf> where TSelf : IBinaryOperation<TSelf>
{
    public static virtual bool IsCommutative()
    {
        return false;
    }

    public static virtual bool IsAssociative()
    {
        return false;
    }

    public static virtual bool IsDistributive<TOther>() where TOther : IBinaryOperation<TOther>
    {
        return false;
    }
}