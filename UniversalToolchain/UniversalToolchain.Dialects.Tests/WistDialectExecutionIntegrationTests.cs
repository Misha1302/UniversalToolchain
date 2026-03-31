using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using NumbersModule.Core;
using StringsModule.Core;
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

    [Test]
    public void FullDialect_RichProgram_RunsWithBothRealBackends()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var example = ResolveExampleDirectory("full-default");
        var code = File.ReadAllText(Path.Combine(example, "program.wist"));

        var result = workflow.ComposeFile(Path.Combine(example, "dialect.wistdialect"));
        using var host = workflow.CreateHost(result);
        var interpreterValue = host.Run(code, "interpreter");
        var compilerValue = host.Run(code, "compiler");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(ToDouble(interpreterValue), Is.EqualTo(15d).Within(1e-9));
            Assert.That(ToDouble(compilerValue), Is.EqualTo(15d).Within(1e-9));
        });
    }


    [Test]
    public void FullNativeDialect_RichProgram_RunsWithBothRealBackends()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var example = ResolveExampleDirectory("full-default-native");
        var code = File.ReadAllText(Path.Combine(example, "program.wist"));

        var result = workflow.ComposeFile(Path.Combine(example, "dialect.wistdialect"));
        using var host = workflow.CreateHost(result);
        var interpreterValue = host.Run(code, "interpreter");
        var compilerValue = host.Run(code, "compiler");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(ToDouble(interpreterValue), Is.EqualTo(15d).Within(1e-9));
            Assert.That(ToDouble(compilerValue), Is.EqualTo(15d).Within(1e-9));
        });
    }


    [Test]
    public void FullDialect_CommentsProgram_RunsWithBothRealBackends()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var example = ResolveExampleDirectory("full-default");

        var result = workflow.ComposeFile(Path.Combine(example, "dialect.wistdialect"));
        using var host = workflow.CreateHost(result);
        var code = """
                   // single line comment
                   let x = 2
                   /* block
                      comment */
                   x + 5
                   """;

        var interpreterValue = host.Run(code, "interpreter");
        var compilerValue = host.Run(code, "compiler");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(ToDouble(interpreterValue), Is.EqualTo(7d).Within(1e-9));
            Assert.That(ToDouble(compilerValue), Is.EqualTo(7d).Within(1e-9));
        });
    }

    [Test]
    public void FullDialect_CSharpInteropProgram_RunsWithBothRealBackends()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var example = ResolveExampleDirectory("full-default");

        var result = workflow.ComposeFile(Path.Combine(example, "dialect.wistdialect"));
        using var host = workflow.CreateHost(result);
        const string code = "NumbersModule.Core.RealNumberImpl.Add(2, 5)";

        var interpreterValue = host.Run(code, "interpreter");
        var compilerValue = host.Run(code, "compiler");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(ToDouble(interpreterValue), Is.EqualTo(7d).Within(1e-9));
            Assert.That(ToDouble(compilerValue), Is.EqualTo(7d).Within(1e-9));
        });
    }


    [Test]
    public void FullDialect_StringsSupport_WorksAcrossBackends()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var example = ResolveExampleDirectory("full-default");

        var result = workflow.ComposeFile(Path.Combine(example, "dialect.wistdialect"));
        using var host = workflow.CreateHost(result);
        const string code = """
                            let left = "http://x"
                            let right: string = "api"
                            left + "/" + right
                            """;

        var interpreterValue = host.Run(code, "interpreter");
        var compilerValue = host.Run(code, "compiler");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(ToStringValue(interpreterValue), Is.EqualTo("http://x/api"));
            Assert.That(ToStringValue(compilerValue), Is.EqualTo("http://x/api"));
        });
    }

    [Test]
    public void RestrictedDialect_DisabledCompilerBackend_IsRejected()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var example = ResolveExampleDirectory("restricted-sandbox");

        var result = workflow.ComposeFile(Path.Combine(example, "dialect.wistdialect"));
        using var host = workflow.CreateHost(result);

        var ex = Assert.Throws<InvalidOperationException>(() => host.GetCore("compiler"));

        Assert.That(ex!.Message, Does.Contain("does not enable the 'compiler' backend"));
    }

    [Test]
    public void RestrictedDialect_ExcludedVariableModules_AreActuallyUnavailable()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var example = ResolveExampleDirectory("restricted-sandbox");

        var result = workflow.ComposeFile(Path.Combine(example, "dialect.wistdialect"));
        using var host = workflow.CreateHost(result);

        var ex = Assert.Catch<Exception>(() => host.Run(File.ReadAllText(Path.Combine(example, "forbidden-program.wist")), "interpreter"));

        Assert.That(ex!.Message, Does.Contain("Variable").Or.Contain("Identifier").Or.Contain("token").IgnoreCase);
    }

    [Test]
    public void RestrictedDialect_ExpressionOnlyProgram_IsAllowedAndExecutable()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var example = ResolveExampleDirectory("restricted-sandbox");

        var result = workflow.ComposeFile(Path.Combine(example, "dialect.wistdialect"));
        using var host = workflow.CreateHost(result);

        var value = host.Run(File.ReadAllText(Path.Combine(example, "program.wist")), "interpreter");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(value, Is.EqualTo(true));
        });
    }

    [Test]
    public void RestrictedDialect_InteropProgram_IsRejected()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var example = ResolveExampleDirectory("restricted-sandbox");

        var result = workflow.ComposeFile(Path.Combine(example, "dialect.wistdialect"));
        using var host = workflow.CreateHost(result);

        var ex = Assert.Catch<Exception>(() => host.Run(File.ReadAllText(Path.Combine(example, "forbidden-interop.wist")), "interpreter"));

        Assert.That(ex!.Message, Does.Contain("interop").Or.Contain("identifier").Or.Contain("token").IgnoreCase);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();
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

    private static string ToStringValue(object? value)
    {
        return value switch
        {
            WistStringImpl stringValue => stringValue.GetValue(),
            string rawString => rawString,
            _ => Thrower.InvalidCast<string>($"Unsupported result value '{value?.GetType().FullName ?? "<null>"}'.")
        };
    }
}