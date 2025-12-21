using System.Reflection;
using ExceptionsManager;
using GenericMath;

namespace Example;

public static class Main
{
    public static void Print(object? obj)
    {
        Console.WriteLine(obj);
    }

    public static string Input()
    {
        return Console.ReadLine().NotNull();
    }

    public static void ImportAssembly(string path)
    {
        Assembly.LoadFile(path);
    }

    public static TSelf Log<TSelf>(ICustomNumber<TSelf, double> a, ICustomNumber<TSelf, double> newBase)
        where TSelf : ICustomNumber<TSelf, double>
    {
        return TSelf.Create(Math.Log(a.GetValue(), newBase.GetValue()));
    }

    public static TSelf Sqrt<TSelf>(ICustomNumber<TSelf, double> x) where TSelf : ICustomNumber<TSelf, double>
    {
        return TSelf.Create(Math.Sqrt(x.GetValue()));
    }
}