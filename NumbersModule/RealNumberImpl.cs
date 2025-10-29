using System.Globalization;
using GenericMath;

namespace NumbersModule;

public readonly struct RealNumberImpl(double value) : ICustomNumber<RealNumberImpl, double>
{
    public static RealNumberImpl Sub(RealNumberImpl a, RealNumberImpl b)
    {
        return new RealNumberImpl(a.GetValue() - b.GetValue());
    }

    public static RealNumberImpl Mul(RealNumberImpl a, RealNumberImpl b)
    {
        return new RealNumberImpl(a.GetValue() * b.GetValue());
    }

    public static RealNumberImpl Add(RealNumberImpl a, RealNumberImpl b)
    {
        return new RealNumberImpl(a.GetValue() + b.GetValue());
    }

    public static RealNumberImpl Div(RealNumberImpl a, RealNumberImpl b)
    {
        return new RealNumberImpl(a.GetValue() / b.GetValue());
    }

    public double GetValue()
    {
        return value;
    }

    public static RealNumberImpl Create(double value)
    {
        return new RealNumberImpl(value);
    }

    public override string ToString()
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    public static bool IsCommutative()
    {
        throw new NotImplementedException();
    }

    public static bool IsAssociative()
    {
        throw new NotImplementedException();
    }

    public static bool IsDistributive<TOther>() where TOther : IBinaryOperation<TOther>
    {
        throw new NotImplementedException();
    }
}