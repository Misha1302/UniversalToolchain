using BenchmarkDotNet.Running;

Console.WriteLine("Enter path to dlls with modules to use: ");
GlobalPath.PathToDlls = Console.ReadLine()!;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

public static class GlobalPath
{
    public static string PathToDlls = null!;
}