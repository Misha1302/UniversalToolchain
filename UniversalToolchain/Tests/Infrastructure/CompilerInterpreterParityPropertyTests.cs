using DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Infrastructure;

[TestFixture]
public class CompilerInterpreterParityPropertyTests
{
    [Test]
    public void SameProgram_ShouldProduceSameResult_InCompilerAndInterpreter_ForArithmeticExpressions()
    {
        AssertParity("let a = 9\nlet b = 4\n(a * b) + (a - b)");
    }

    [Test]
    public void SameProgram_ShouldProduceSameResult_InCompilerAndInterpreter_ForNestedConditions()
    {
        AssertParity("let x = 7\nif x > 5 (if x < 10 (42) else (24)) else (0)");
    }

    [Test]
    public void SameProgram_ShouldProduceSameResult_InCompilerAndInterpreter_ForLoopAccumulation()
    {
        AssertParity("let i = 0\nlet sum = 0\nwhile i < 6 (sum = sum + i\ni = i + 1)\nsum");
    }

    [Test]
    public void SameProgram_ShouldProduceSameResult_InCompilerAndInterpreter_ForVariableShadowing()
    {
        AssertParity("let x = 10\nlet x = x + 1\nx + 5");
    }

    [Test]
    public void SameProgram_ShouldPreserveShortCircuitSemantics_InBothBackends()
    {
        AssertParity("let x = 0\nlet safe = (x != 0) and (10 / x > 1)\nif safe (1) else (2)");
    }

    [Test]
    public void InvalidProgram_ShouldFailConsistently_InBothBackends()
    {
        const string code = "let x =\n";
        var compilerException = Assert.Catch(() => Execute(code, "compiler"));
        var interpreterException = Assert.Catch(() => Execute(code, "interpreter"));

        Assert.That(interpreterException!.GetType(), Is.EqualTo(compilerException!.GetType()));
        Assert.That(interpreterException.Message.Split('\n')[0], Is.EqualTo(compilerException.Message.Split('\n')[0]));
    }

    [Test]
    public void GeneratedPrograms_ShouldMaintainSemanticParity_AcrossBackends()
    {
        const int seed = 1337;
        var random = new Random(seed);

        for (var i = 0; i < 60; i++)
        {
            var program = GenerateProgram(random, i);
            var compilerResult = Execute(program, "compiler");
            var interpreterResult = Execute(program, "interpreter");
            Assert.That(interpreterResult, Is.EqualTo(compilerResult), $"Seed={seed}; Case={i}; Program:\n{program}");
        }
    }

    private static string GenerateProgram(Random random, int index)
    {
        var a = random.Next(-5, 12);
        var b = random.Next(1, 8);

        return (index % 2) switch
        {
            0 => $"let a = {a}\nlet b = {b}\n(a + b) * (a - b)",
            1 => $"let i = 0\nlet acc = {random.Next(0, 3)}\nwhile i < {random.Next(1, 6)} (acc = acc + {b}\ni = i + 1)\nacc",
            _ => $"let x = {a}\nlet y = {b}\n(x * y) - y"
        };
    }

    private static object? Execute(string code, string mode)
    {
        using var provider = BuildProvider();
        var core = provider.GetServices<ICoreRunnable>()
            .First(r => r.GetType().IsGenericType &&
                        r.GetType().GetGenericTypeDefinition() == typeof(BasicCoreImpl<>) &&
                        ((mode == "compiler" && r.GetType().GetGenericArguments()[0] == typeof(DynamicMethod)) ||
                         (mode == "interpreter" && r.GetType().GetGenericArguments()[0] == typeof(IAbstractIR))));
        return core.Run(code);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddWistServices();
        return services.BuildServiceProvider();
    }

    private static void AssertParity(string code)
    {
        Assert.That(Execute(code, "interpreter"), Is.EqualTo(Execute(code, "compiler")));
    }
}
