using System.Globalization;
using System.Reflection;
using ExceptionsManager;

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

    public static double ToNum(string str)
    {
        return double.Parse(str, NumberStyles.Any);
    }

    public static void ImportAssembly(string path)
    {
        Assembly.LoadFile(path);
    }

    public static double Log(double x, double b)
    {
        return Math.Log(x, b);
    }
}