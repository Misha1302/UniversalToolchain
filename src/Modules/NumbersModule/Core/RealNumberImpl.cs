namespace NumbersModule.Core;

public readonly struct RealNumberImpl(double value) : ICustomNumber<RealNumberImpl, double>, IComparable<RealNumberImpl>
{
    public static RealNumberImpl Sub(RealNumberImpl a, RealNumberImpl b) => new(a.GetValue() - b.GetValue());

    public static RealNumberImpl Mul(RealNumberImpl a, RealNumberImpl b) => new(a.GetValue() * b.GetValue());

    public static RealNumberImpl Add(RealNumberImpl a, RealNumberImpl b) => new(a.GetValue() + b.GetValue());

    public static RealNumberImpl Div(RealNumberImpl a, RealNumberImpl b) => new(a.GetValue() / b.GetValue());

    public double GetValue() => value;

    public static RealNumberImpl Create(double value) => new(value);

    public override string ToString() => value.ToString(CultureInfo.InvariantCulture);

    public int CompareTo(RealNumberImpl other)
    {
        var v1 = GetValue();
        var v2 = other.GetValue();
        if (Math.Sign(v1) == Math.Sign(v2))
        {
            var a1 = Math.Abs(v1);
            var a2 = Math.Abs(v2);

            const double error = 1e-10;
            if (Math.Abs(a1 - a2) < error)
                return 0;
            if (a1 > double.Epsilon && a2 > double.Epsilon && Math.Max(a1, a2) / Math.Min(a1, a2) < 1 + error)
                return 0;
        }

        return GetValue().CompareTo(other.GetValue());
    }
}