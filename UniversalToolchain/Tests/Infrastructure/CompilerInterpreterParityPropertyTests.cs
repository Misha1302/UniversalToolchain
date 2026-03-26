using DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Infrastructure;

[TestFixture]
public class CompilerInterpreterParityPropertyTests
{
    [Test]
    public void SameProgram_ShouldProduceSameResult_InCompilerAndInterpreter_ForArithmeticExpressions()
        => AssertParity("let a = 9\nlet b = 4\n(a * b) + (a - b)");

    [Test]
    public void SameProgram_ShouldProduceSameResult_InCompilerAndInterpreter_ForNestedConditions()
        => AssertParity("let x = 7\nif x > 5 (if x < 10 (42) else (24)) else (0)");

    [Test]
    public void SameProgram_ShouldProduceSameResult_InCompilerAndInterpreter_ForLoopAccumulation()
        => AssertParity("let i = 0\nlet sum = 0\nwhile i < 6 (sum = sum + i\ni = i + 1)\nsum");

    [Test]
    public void SameProgram_ShouldProduceSameResult_InCompilerAndInterpreter_ForScopedVariableRebinding()
        => AssertParity("let x = 10\nx = x + 2\nlet y = x * 3\ny - x");

    [Test]
    public void SameProgram_ShouldPreserveShortCircuitSemantics_InBothBackends()
        => AssertParity("let x = 0\nlet safe = (x != 0) and (10 / x > 1)\nif safe (1) else (2)");

    [Test]
    public void InvalidProgram_ShouldFailConsistently_InBothBackends()
    {
        const string code = "let x =\n";

        var compilerException = Assert.Catch(() => Execute(code, BackendMode.Compiler));
        var interpreterException = Assert.Catch(() => Execute(code, BackendMode.Interpreter));

        Assert.That(interpreterException, Is.Not.Null);
        Assert.That(compilerException, Is.Not.Null);
        Assert.That(interpreterException!.GetType(), Is.EqualTo(compilerException!.GetType()));
        Assert.That(interpreterException.Message.Split('\n')[0], Is.EqualTo(compilerException.Message.Split('\n')[0]));
    }

    [Test]
    public void GeneratedPrograms_ShouldMaintainSemanticParity_AcrossBackends()
    {
        const int seed = 1302;
        var random = new Random(seed);

        for (var i = 0; i < 70; i++)
        {
            var program = GenerateBoundedProgram(random, i);
            var compilerResult = Execute(program, BackendMode.Compiler);
            var interpreterResult = Execute(program, BackendMode.Interpreter);

            Assert.That(interpreterResult, Is.EqualTo(compilerResult), $"Seed={seed}; Case={i}; Program:\n{program}");
        }
    }

    private static string GenerateBoundedProgram(Random random, int caseIndex)
    {
        var a = random.Next(-7, 11);
        var b = random.Next(1, 7);
        var c = random.Next(1, 5);

        return (caseIndex % 4) switch
        {
            0 => $"let a = {a}\nlet b = {b}\n(a + b) * (a - b)",
            1 => $"let x = {a}\nlet y = {b}\n(x + y) - y",
            2 => $"let i = 0\nlet acc = {c}\nwhile i < {random.Next(1, 6)} (acc = acc + {b}\ni = i + 1)\nacc",
            _ => $"let x = {a}\nx = x + {b}\nx * {c}"
        };
    }

    private static object? Execute(string code, BackendMode mode)
    {
        using var provider = BuildProvider();
        var core = provider.GetServices<ICoreRunnable>().First(x =>
            x.GetType().IsGenericType &&
            x.GetType().GetGenericTypeDefinition() == typeof(BasicCoreImpl<>) &&
            (mode == BackendMode.Compiler
                ? x.GetType().GetGenericArguments()[0] == typeof(DynamicMethod)
                : x.GetType().GetGenericArguments()[0] == typeof(IAbstractIR)));

        return core.Run(code);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddWistServices();
        return services.BuildServiceProvider();
    }

    private static void AssertParity(string code)
        => Assert.That(Execute(code, BackendMode.Interpreter), Is.EqualTo(Execute(code, BackendMode.Compiler)));

    private enum BackendMode
    {
        Compiler,
        Interpreter
    }
}
