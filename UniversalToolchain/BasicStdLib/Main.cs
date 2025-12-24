using GenericMath;
using JetBrains.Annotations;
using NumbersModule;

namespace BasicStdLib;

[UsedImplicitly]
public static class Main
{
    public static void Print(object? obj)
    {
        Console.WriteLine(obj);
    }

    public static RealNumberImpl Get42()
    {
        return new RealNumberImpl(42);
    }

    public static TSelf Log<TSelf>(TSelf a, TSelf newBase) where TSelf : ICustomNumber<TSelf, double>
    {
        return TSelf.Create(Math.Log(a.GetValue(), newBase.GetValue()));
    }

    public static TSelf Sqrt<TSelf>(TSelf a) where TSelf : ICustomNumber<TSelf, double>
    {
        return TSelf.Create(Math.Sqrt(a.GetValue()));
    }

    public static TSelf Floor<TSelf>(TSelf a) where TSelf : ICustomNumber<TSelf, double>
    {
        return TSelf.Create(Math.Floor(a.GetValue()));
    }

    public static TSelf Ceiling<TSelf>(TSelf a) where TSelf : ICustomNumber<TSelf, double>
    {
        return TSelf.Create(Math.Ceiling(a.GetValue()));
    }

    public static TSelf Abs<TSelf>(TSelf x) where TSelf : ICustomNumber<TSelf, double>
    {
        return TSelf.Create(Math.Abs(x.GetValue()));
    }

    public static TSelf Max<TSelf>(TSelf x, TSelf y) where TSelf : ICustomNumber<TSelf, double>
    {
        return TSelf.Create(Math.Max(x.GetValue(), y.GetValue()));
    }

    public static TSelf Min<TSelf>(TSelf x, TSelf y) where TSelf : ICustomNumber<TSelf, double>
    {
        return TSelf.Create(Math.Min(x.GetValue(), y.GetValue()));
    }

    public static TSelf Pow<TSelf>(TSelf x, TSelf y) where TSelf : ICustomNumber<TSelf, double>
    {
        return TSelf.Create(Math.Pow(x.GetValue(), y.GetValue()));
    }

    public static TSelf Sin<TSelf>(TSelf x) where TSelf : ICustomNumber<TSelf, double>
    {
        return TSelf.Create(Math.Sin(x.GetValue()));
    }

    public static TSelf Cos<TSelf>(TSelf x) where TSelf : ICustomNumber<TSelf, double>
    {
        return TSelf.Create(Math.Cos(x.GetValue()));
    }

    public static TSelf Round<TSelf>(TSelf x) where TSelf : ICustomNumber<TSelf, double>
    {
        return TSelf.Create(Math.Round(x.GetValue()));
    }

    public static void LoadStdLibToThisAssembly()
    {
    }
}