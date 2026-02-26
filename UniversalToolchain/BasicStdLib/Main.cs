using System.Runtime.CompilerServices;
using ExceptionsManager;
using GenericMath;
using JetBrains.Annotations;
using NumbersModule.Core;

namespace BasicStdLib;

[UsedImplicitly]
public static class Main
{
    [UsedImplicitly]
    public static void Print(object? obj)
    {
        Console.WriteLine(obj);
    }

    [UsedImplicitly]
    public static RealNumberImpl Get42() => new(42);

    [UsedImplicitly]
    public static TSelf Log<TSelf>(TSelf a, TSelf newBase) where TSelf : ICustomNumber<TSelf, double> => TSelf.Create(Math.Log(a.GetValue(), newBase.GetValue()));

    [UsedImplicitly]
    public static TSelf Sqrt<TSelf>(TSelf a) where TSelf : ICustomNumber<TSelf, double> => TSelf.Create(Math.Sqrt(a.GetValue()));

    [UsedImplicitly]
    public static TSelf Floor<TSelf>(TSelf a) where TSelf : ICustomNumber<TSelf, double> => TSelf.Create(Math.Floor(a.GetValue()));

    [UsedImplicitly]
    public static TSelf Ceiling<TSelf>(TSelf a) where TSelf : ICustomNumber<TSelf, double> => TSelf.Create(Math.Ceiling(a.GetValue()));

    [UsedImplicitly]
    public static TSelf Abs<TSelf>(TSelf x) where TSelf : ICustomNumber<TSelf, double> => TSelf.Create(Math.Abs(x.GetValue()));

    [UsedImplicitly]
    public static TSelf Max<TSelf>(TSelf x, TSelf y) where TSelf : ICustomNumber<TSelf, double> => TSelf.Create(Math.Max(x.GetValue(), y.GetValue()));

    [UsedImplicitly]
    public static TSelf Min<TSelf>(TSelf x, TSelf y) where TSelf : ICustomNumber<TSelf, double> => TSelf.Create(Math.Min(x.GetValue(), y.GetValue()));

    [UsedImplicitly]
    public static TSelf Pow<TSelf>(TSelf x, TSelf y) where TSelf : ICustomNumber<TSelf, double> => TSelf.Create(Math.Pow(x.GetValue(), y.GetValue()));

    [UsedImplicitly]
    public static TSelf Sin<TSelf>(TSelf x) where TSelf : ICustomNumber<TSelf, double> => TSelf.Create(Math.Sin(x.GetValue()));

    [UsedImplicitly]
    public static TSelf Cos<TSelf>(TSelf x) where TSelf : ICustomNumber<TSelf, double> => TSelf.Create(Math.Cos(x.GetValue()));

    [UsedImplicitly]
    public static TSelf Round<TSelf>(TSelf x) where TSelf : ICustomNumber<TSelf, double> => TSelf.Create(Math.Round(x.GetValue()));

    [UsedImplicitly]
    public static int ToInt<TSelf>(TSelf x)
    {
        if (typeof(TSelf) == typeof(int)) return Unsafe.BitCast<TSelf, int>(x);
        if (typeof(TSelf) == typeof(float)) return (int)Unsafe.BitCast<TSelf, float>(x);
        if (typeof(TSelf) == typeof(double)) return (int)Unsafe.BitCast<TSelf, double>(x);
        if (typeof(TSelf) == typeof(decimal)) return (int)Unsafe.BitCast<TSelf, decimal>(x);
        if (typeof(TSelf) == typeof(string)) return int.Parse(Unsafe.BitCast<TSelf, string>(x));
        return Thrower.InvalidOpEx<int>($"Unknown type {typeof(TSelf)}");
    }

    [UsedImplicitly]
    public static float ToFloat<TSelf>(TSelf x)
    {
        if (typeof(TSelf) == typeof(int)) return Unsafe.BitCast<TSelf, int>(x);
        if (typeof(TSelf) == typeof(float)) return Unsafe.BitCast<TSelf, float>(x);
        if (typeof(TSelf) == typeof(double)) return (float)Unsafe.BitCast<TSelf, double>(x);
        if (typeof(TSelf) == typeof(decimal)) return (float)Unsafe.BitCast<TSelf, decimal>(x);
        if (typeof(TSelf) == typeof(string)) return float.Parse(Unsafe.BitCast<TSelf, string>(x));
        return Thrower.InvalidOpEx<float>($"Unknown type {typeof(TSelf)}");
    }

    [UsedImplicitly]
    public static double ToDouble<TSelf>(TSelf x)
    {
        if (typeof(TSelf) == typeof(int)) return Unsafe.BitCast<TSelf, int>(x);
        if (typeof(TSelf) == typeof(float)) return Unsafe.BitCast<TSelf, float>(x);
        if (typeof(TSelf) == typeof(double)) return Unsafe.BitCast<TSelf, double>(x);
        if (typeof(TSelf) == typeof(decimal)) return (double)Unsafe.BitCast<TSelf, decimal>(x);
        if (typeof(TSelf) == typeof(string)) return double.Parse(Unsafe.BitCast<TSelf, string>(x));
        return Thrower.InvalidOpEx<double>($"Unknown type {typeof(TSelf)}");
    }

    [UsedImplicitly]
    public static decimal ToDecimal<TSelf>(TSelf x)
    {
        if (typeof(TSelf) == typeof(int)) return Unsafe.BitCast<TSelf, int>(x);
        if (typeof(TSelf) == typeof(float)) return (decimal)Unsafe.BitCast<TSelf, float>(x);
        if (typeof(TSelf) == typeof(double)) return (decimal)Unsafe.BitCast<TSelf, double>(x);
        if (typeof(TSelf) == typeof(decimal)) return Unsafe.BitCast<TSelf, decimal>(x);
        if (typeof(TSelf) == typeof(string)) return decimal.Parse(Unsafe.BitCast<TSelf, string>(x));
        return Thrower.InvalidOpEx<decimal>($"Unknown type {typeof(TSelf)}");
    }
}