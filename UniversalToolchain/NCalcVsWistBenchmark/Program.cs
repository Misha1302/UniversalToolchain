using BenchmarkDotNet.Running;
using NCalcVsWistBenchmark;

Console.WriteLine("Enter path to dlls with modules to use: ");
GlobalPath.PathToDlls = Console.ReadLine()!;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

namespace NCalcVsWistBenchmark
{
    public static class GlobalPath
    {
        public static string PathToDlls = null!;
    }
}