using Microsoft.Extensions.DependencyInjection;
using NumbersModule.Core;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

internal sealed class ModulePipelineTestHelper : IDisposable
{
    public static readonly string[] FullUniversalModules =
    [
        "Whitespaces", "SemicolonAsNewLine", "Comments", "Numbers", "Identifier", "Arithmetic", "Equality",
        "Conditions", "ComparisonConditions", "BooleanConditions", "Loops", "Variables", "Scopes", "Labels",
        "InternalPreprocessorLexemes", "CSharpInterop"
    ];

    private readonly ServiceProvider _provider;
    private readonly WistDialectExecutionWorkflow _workflow;

    public ModulePipelineTestHelper()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();
        _provider = services.BuildServiceProvider();
        _workflow = _provider.GetRequiredService<WistDialectExecutionWorkflow>();
    }

    public void Dispose() => _provider.Dispose();

    public string BuildDialectText(string name, IEnumerable<string> modules, IEnumerable<string>? optimizers = null, IEnumerable<string>? backends = null)
    {
        var modulesLine = string.Join(',', modules);
        var optimizerLine = optimizers == null ? string.Empty : $"\nenable {string.Join(',', optimizers)}";
        var backendLine = $"\nbackend {string.Join(',', backends ?? ["compiler", "interpreter"])}";
        return $"dialect {name}\nuse {modulesLine}{optimizerLine}{backendLine}";
    }

    public WistDialectExecutionHost CreateHost(IEnumerable<string> modules, IEnumerable<string>? optimizers = null, IEnumerable<string>? backends = null)
    {
        var composition = _workflow.ComposeText(BuildDialectText("Inline", modules, optimizers, backends), "inline");
        if (!composition.IsSuccess)
            throw new InvalidOperationException(composition.ToDeterministicText());

        return _workflow.CreateHost(composition);
    }

    public DialectFrameworkCompositionResult Compose(IEnumerable<string> modules, IEnumerable<string>? optimizers = null, IEnumerable<string>? backends = null)
        => _workflow.ComposeText(BuildDialectText("Inline", modules, optimizers, backends), "inline");

    public object? Execute(string code, string mode, IEnumerable<string> modules, IEnumerable<string>? optimizers = null)
    {
        using var host = CreateHost(modules, optimizers, [mode]);
        return host.Run(code, mode);
    }

    public object? ExecuteCompiler(string code, IEnumerable<string> modules, IEnumerable<string>? optimizers = null)
        => Execute(code, "compiler", modules, optimizers);

    public object? ExecuteInterpreter(string code, IEnumerable<string> modules, IEnumerable<string>? optimizers = null)
        => Execute(code, "interpreter", modules, optimizers);

    public (object? Compiler, object? Interpreter) ExecuteBoth(string code, IEnumerable<string> modules, IEnumerable<string>? optimizers = null)
    {
        using var host = CreateHost(modules, optimizers, ["compiler", "interpreter"]);
        var interpreter = host.Run(code, "interpreter");
        var compiler = host.Run(code, "compiler");
        return (compiler, interpreter);
    }

    public static double AsNumber(object? value)
        => value switch
        {
            RealNumberImpl n => n.GetValue(),
            int i => i,
            long l => l,
            float f => f,
            double d => d,
            decimal m => (double)m,
            _ => throw new InvalidCastException($"Cannot convert '{value?.GetType().Name ?? "null"}' to number.")
        };

    public static bool AsBool(object? value)
        => value switch
        {
            bool b => b,
            int i => i != 0,
            RealNumberImpl n => Math.Abs(n.GetValue()) > double.Epsilon,
            _ => throw new InvalidCastException($"Cannot convert '{value?.GetType().Name ?? "null"}' to bool.")
        };

    public static void AssertParity(object? compiler, object? interpreter)
    {
        if (compiler is null || interpreter is null)
        {
            Assert.That(compiler, Is.EqualTo(interpreter));
            return;
        }

        if (compiler is bool || interpreter is bool)
        {
            Assert.That(AsBool(compiler), Is.EqualTo(AsBool(interpreter)));
            return;
        }

        Assert.That(AsNumber(compiler), Is.EqualTo(AsNumber(interpreter)).Within(1e-9));
    }

    private static void AssertSemanticEqual(object? left, object? right)
    {
        if (left is null || right is null)
        {
            Assert.That(left, Is.EqualTo(right));
            return;
        }

        if (left is bool || right is bool)
        {
            Assert.That(AsBool(left), Is.EqualTo(AsBool(right)));
            return;
        }

        Assert.That(AsNumber(left), Is.EqualTo(AsNumber(right)).Within(1e-9));
    }

    private static void AssertSemanticNotEqual(object? left, object? right)
    {
        if (left is null || right is null)
        {
            Assert.That(left, Is.Not.EqualTo(right));
            return;
        }

        if (left is bool || right is bool)
        {
            Assert.That(AsBool(left), Is.Not.EqualTo(AsBool(right)));
            return;
        }

        Assert.That(Math.Abs(AsNumber(left) - AsNumber(right)), Is.GreaterThan(1e-9));
    }

    public void ExecuteEquivalent(string a, string b, IEnumerable<string> modules, IEnumerable<string>? optimizers = null)
    {
        var resultA = ExecuteBoth(a, modules, optimizers);
        var resultB = ExecuteBoth(b, modules, optimizers);
        AssertParity(resultA.Compiler, resultA.Interpreter);
        AssertParity(resultB.Compiler, resultB.Interpreter);
        AssertSemanticEqual(resultA.Compiler, resultB.Compiler);
        AssertSemanticEqual(resultA.Interpreter, resultB.Interpreter);
    }

    public void ExecuteDifferent(string a, string b, IEnumerable<string> modules, IEnumerable<string>? optimizers = null)
    {
        var resultA = ExecuteBoth(a, modules, optimizers);
        var resultB = ExecuteBoth(b, modules, optimizers);
        AssertParity(resultA.Compiler, resultA.Interpreter);
        AssertParity(resultB.Compiler, resultB.Interpreter);
        AssertSemanticNotEqual(resultA.Compiler, resultB.Compiler);
        AssertSemanticNotEqual(resultA.Interpreter, resultB.Interpreter);
    }

    public void AssertFails(string code, IEnumerable<string> modules, params string[] expectedAnyOf)
        => AssertFails(code, modules, null, expectedAnyOf);

    public void AssertFails<TException>(string code, IEnumerable<string> modules, params string[] expectedAnyOf)
        where TException : Exception
        => AssertFails(code, modules, typeof(TException), expectedAnyOf);

    private void AssertFails(string code, IEnumerable<string> modules, Type? expectedExceptionType, params string[] expectedAnyOf)
    {
        var compilerException = Assert.Catch(() => _ = ExecuteCompiler(code, modules));
        var interpreterException = Assert.Catch(() => _ = ExecuteInterpreter(code, modules));

        var compilerMessage = compilerException?.Message ?? "<no message>";
        var interpreterMessage = interpreterException?.Message ?? "<no message>";
        var combinedMessage = $"Compiler error: {compilerMessage}{Environment.NewLine}Interpreter error: {interpreterMessage}";

        Assert.That(compilerException, Is.Not.Null, $"Expected compiler execution to fail. {combinedMessage}");
        Assert.That(interpreterException, Is.Not.Null, $"Expected interpreter execution to fail. {combinedMessage}");

        if (expectedExceptionType != null)
        {
            Assert.That(compilerException, Is.InstanceOf(expectedExceptionType), combinedMessage);
            Assert.That(interpreterException, Is.InstanceOf(expectedExceptionType), combinedMessage);
        }

        var nonEmptyFragments = expectedAnyOf.Where(fragment => !string.IsNullOrWhiteSpace(fragment)).ToArray();
        if (nonEmptyFragments.Length == 0)
            return;

        AssertMessageHasAnyFragment(compilerMessage, nonEmptyFragments, "compiler", combinedMessage);
        AssertMessageHasAnyFragment(interpreterMessage, nonEmptyFragments, "interpreter", combinedMessage);
    }

    private static void AssertMessageHasAnyFragment(string message, IEnumerable<string> expectedAnyOf, string backend, string combinedMessage)
    {
        var expectedList = expectedAnyOf.ToArray();
        var matchFound = expectedList.Any(fragment => message.Contains(fragment, StringComparison.OrdinalIgnoreCase));

        Assert.That(
            matchFound,
            Is.True,
            $"Expected {backend} error to contain any of: [{string.Join(", ", expectedList)}]. {combinedMessage}");
    }
}
