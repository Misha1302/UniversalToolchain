using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;
using Tests.Infrastructure;

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

    public (BackendExecutionResult Compiler, BackendExecutionResult Interpreter) ExecuteBoth(string code, IEnumerable<string> modules, IEnumerable<string>? optimizers = null)
    {
        using var host = CreateHost(modules, optimizers, ["compiler", "interpreter"]);
        var interpreter = ExecuteWithResult(() => host.Run(code, "interpreter"));
        var compiler = ExecuteWithResult(() => host.Run(code, "compiler"));
        return (compiler, interpreter);
    }

    public static double AsNumber(object? value) => BackendParityInfrastructure.AsNumber(value);

    public static bool AsBool(object? value) => BackendParityInfrastructure.AsBool(value);

    public static void AssertParity(BackendExecutionResult compiler, BackendExecutionResult interpreter)
        => BackendParityInfrastructure.AssertSemanticParity(compiler, interpreter);

    public static void AssertParity(object? compiler, object? interpreter)
        => BackendParityInfrastructure.AssertSemanticParity(BackendExecutionResult.Success(compiler), BackendExecutionResult.Success(interpreter));

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
        AssertSemanticEqual(resultA.Compiler.Value, resultB.Compiler.Value);
        AssertSemanticEqual(resultA.Interpreter.Value, resultB.Interpreter.Value);
    }

    public void ExecuteDifferent(string a, string b, IEnumerable<string> modules, IEnumerable<string>? optimizers = null)
    {
        var resultA = ExecuteBoth(a, modules, optimizers);
        var resultB = ExecuteBoth(b, modules, optimizers);
        AssertParity(resultA.Compiler, resultA.Interpreter);
        AssertParity(resultB.Compiler, resultB.Interpreter);
        AssertSemanticNotEqual(resultA.Compiler.Value, resultB.Compiler.Value);
        AssertSemanticNotEqual(resultA.Interpreter.Value, resultB.Interpreter.Value);
    }

    public void AssertFailsContaining(string code, IEnumerable<string> modules, string expectedFragment)
    {
        var result = ExecuteBoth(code, modules);
        AssertParity(result.Compiler, result.Interpreter);

        Assert.That(result.Compiler.IsSuccess, Is.False);
        Assert.That(result.Compiler.Exception, Is.Not.Null);
        Assert.That(result.Interpreter.Exception, Is.Not.Null);
        Assert.That(result.Compiler.Exception!.ToString().Contains(expectedFragment, StringComparison.OrdinalIgnoreCase), Is.True);
        Assert.That(result.Interpreter.Exception!.ToString().Contains(expectedFragment, StringComparison.OrdinalIgnoreCase), Is.True);
    }

    public void AssertCompilerAndInterpreterFailSameWay(string code, IEnumerable<string> modules)
    {
        var result = ExecuteBoth(code, modules);
        AssertParity(result.Compiler, result.Interpreter);
        Assert.That(result.Compiler.IsSuccess, Is.False);
    }

    public void AssertParityAndValue(string code, IEnumerable<string> modules, double expected)
    {
        var (compiler, interpreter) = ExecuteBoth(code, modules);
        AssertParity(compiler, interpreter);
        Assert.That(AsNumber(compiler.Value), Is.EqualTo(expected).Within(1e-9));
    }

    private static BackendExecutionResult ExecuteWithResult(Func<object?> run)
    {
        try
        {
            return BackendExecutionResult.Success(run());
        }
        catch (Exception ex)
        {
            return BackendExecutionResult.Failure(ex);
        }
    }
}
