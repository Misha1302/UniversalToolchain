using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using NumbersModule.Core;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class WistDialectExecutionIntegrationTests
{
    [Test]
    public void MinimalDialect_ArithmeticProgram_RunsThroughRealExecutionPath()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var example = ResolveExampleDirectory("minimal-arithmetic");

        var result = workflow.ComposeFile(Path.Combine(example, "dialect.wistdialect"));
        using var host = workflow.CreateHost(result);
        var value = host.Run(File.ReadAllText(Path.Combine(example, "program.wist")), "interpreter");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(ToDouble(value), Is.EqualTo(14d).Within(1e-9));
        });
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        return services.BuildServiceProvider();
    }

    private static string ResolveExampleDirectory(string name)
    {
        var path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Dialects", "examples", "wist", name));
        if (!Directory.Exists(path))
            Thrower.FileNotFound(path);

        return path;
    }

    private static double ToDouble(object? value)
    {
        return value switch
        {
            RealNumberImpl number => number.GetValue(),
            int intValue => intValue,
            long longValue => longValue,
            double doubleValue => doubleValue,
            float floatValue => floatValue,
            decimal decimalValue => (double)decimalValue,
            _ => Thrower.InvalidCast<double>($"Unsupported result value '{value?.GetType().FullName ?? "<null>"}'.")
        };
    }
}
