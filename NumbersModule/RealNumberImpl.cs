// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

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
}