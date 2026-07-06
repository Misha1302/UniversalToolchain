using System.Runtime.CompilerServices;

namespace UniversalToolchain.Benchmarks;

internal static class FormulaBenchmarkData
{
    public const int DataSize = 4096;

    public static (double[] A, double[] B, double[] C) CreateInputs()
    {
        var random = new Random(42);
        var a = CreateArray(random);
        var b = CreateArray(random);
        var c = CreateArray(random);
        return (a, b, c);
    }

    public static string WistFormula(FormulaWorkload workload) => workload switch
    {
        FormulaWorkload.SimpleArithmetic => "A + B * C / 5.0",
        FormulaWorkload.DeepArithmetic => "(((A * 1.1 + B) * 1.2 + C) / 1.3)",
        FormulaWorkload.RepeatedSubexpressions => "((A * B) + (A * B) + (A * B) + C) / 3.0",
        _ => throw new ArgumentOutOfRangeException(nameof(workload), workload, null)
    };

    public static string NCalcFormula(FormulaWorkload workload) => workload switch
    {
        FormulaWorkload.SimpleArithmetic => "[A] + [B] * [C] / 5.0",
        FormulaWorkload.DeepArithmetic => "((([A] * 1.1 + [B]) * 1.2 + [C]) / 1.3)",
        FormulaWorkload.RepeatedSubexpressions => "(([A] * [B]) + ([A] * [B]) + ([A] * [B]) + [C]) / 3.0",
        _ => throw new ArgumentOutOfRangeException(nameof(workload), workload, null)
    };

    public static Func<double, double, double, double> CSharpDelegate(FormulaWorkload workload) => workload switch
    {
        FormulaWorkload.SimpleArithmetic => SimpleArithmetic,
        FormulaWorkload.DeepArithmetic => DeepArithmetic,
        FormulaWorkload.RepeatedSubexpressions => RepeatedSubexpressions,
        _ => throw new ArgumentOutOfRangeException(nameof(workload), workload, null)
    };

    public static void AssertClose(double expected, double actual, string implementation, int index)
    {
        const double absoluteEpsilon = 1e-9;
        const double relativeEpsilon = 1e-12;
        var delta = Math.Abs(expected - actual);
        var scale = Math.Max(1.0, Math.Max(Math.Abs(expected), Math.Abs(actual)));
        var tolerance = Math.Max(absoluteEpsilon, relativeEpsilon * scale);

        if (delta > tolerance)
        {
            throw new InvalidOperationException(
                $"Benchmark parity failed for {implementation} at index {index}: expected {expected}, actual {actual}, tolerance {tolerance}.");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double SimpleArithmetic(double a, double b, double c) => a + b * c / 5.0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double DeepArithmetic(double a, double b, double c) => ((a * 1.1 + b) * 1.2 + c) / 1.3;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double RepeatedSubexpressions(double a, double b, double c) => (a * b + a * b + a * b + c) / 3.0;

    private static double[] CreateArray(Random random)
    {
        var values = new double[DataSize];
        for (var i = 0; i < values.Length; i++)
            values[i] = 0.1 + random.NextDouble() * 999.9;

        return values;
    }
}
