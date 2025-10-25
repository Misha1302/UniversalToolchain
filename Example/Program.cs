// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using System.Globalization;
using System.Reflection;
using ExceptionsManager;

var core = new BasicCoreImpl(
    () => new BasicLexerImpl(),
    () => new BasicParserImpl(),
    () => new BasicBytecodeBytecodeTranslatorImpl(),
    () => new BasicInterpreterImpl(),
    [
        new ScopesModuleImpl(), new NumbersModuleImpl(), new WhitespaceModuleImpl(), new ArithmeticModuleImpl(),
        new ExecutorDebugLogger(), new CSharpInteropModuleImpl()
    ]
);

var result = core.Execute(
    """
    Main.Print(2 + 3)
    Main.Print(Math.Sqrt(2 + 3))
    Main.Print(Math.Sqrt(2 + 3) * Math.Sqrt(5))
    Main.Print(Main.Log(2.718281828459045, 3.141592653589793))
    """
);

Console.WriteLine(result);


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