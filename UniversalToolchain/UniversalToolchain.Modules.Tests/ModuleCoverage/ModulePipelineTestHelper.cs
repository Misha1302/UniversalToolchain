using System.Text;
using Microsoft.Extensions.DependencyInjection;
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
            throw new InvalidOperationException(UniversalToolchain.Dialects.Integration.DialectCompositionExplanationFormatter.FormatDeterministic(UniversalToolchain.Dialects.Integration.DialectCompositionExplanationProjector.Project(composition)));

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

    public static double AsNumber(object? value) => BackendParityInfrastructure.AsNumber(value);

    public static bool AsBool(object? value) => BackendParityInfrastructure.AsBool(value);

    public static void AssertParity(object? compiler, object? interpreter)
        => BackendParityInfrastructure.AssertSemanticParity(
            BackendExecutionResult.Success(compiler),
            BackendExecutionResult.Success(interpreter));

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

    public void AssertFailsContaining(string code, IEnumerable<string> modules, string expectedFragment)
    {
        var dialectText = BuildDialectText("Inline", modules, null, ["compiler", "interpreter"]);
        var (compilerResult, interpreterResult) = BackendParityInfrastructure.RunBoth(dialectText, code);

        Assert.That(compilerResult.IsSuccess, Is.False);
        Assert.That(interpreterResult.IsSuccess, Is.False);

        var compilerExceptionText = GetComparableException(compilerResult.Exception!).ToString();
        var interpreterExceptionText = GetComparableException(interpreterResult.Exception!).ToString();
        Assert.That(compilerExceptionText.Contains(expectedFragment, StringComparison.OrdinalIgnoreCase), Is.True);
        Assert.That(interpreterExceptionText.Contains(expectedFragment, StringComparison.OrdinalIgnoreCase), Is.True);
    }

    public void AssertFails(string code, IEnumerable<string> modules)
    {
        var dialectText = BuildDialectText("Inline", modules, null, ["compiler", "interpreter"]);
        var (compilerResult, interpreterResult) = BackendParityInfrastructure.RunBoth(dialectText, code);

        Assert.That(compilerResult.IsSuccess, Is.False);
        Assert.That(interpreterResult.IsSuccess, Is.False);
    }

    public void AssertCompilerAndInterpreterFailSameWay(string code, IEnumerable<string> modules)
    {
        var dialectText = BuildDialectText("Inline", modules, null, ["compiler", "interpreter"]);
        var (compilerResult, interpreterResult) = BackendParityInfrastructure.RunBoth(dialectText, code);

        Assert.That(compilerResult.IsSuccess, Is.False);
        Assert.That(interpreterResult.IsSuccess, Is.False);

        var comparableCompilerException = GetComparableException(compilerResult.Exception!);
        var comparableInterpreterException = GetComparableException(interpreterResult.Exception!);
        Assert.That(comparableCompilerException.GetType(), Is.EqualTo(comparableInterpreterException.GetType()));
        Assert.That(GetInvariantMessageFragment(comparableCompilerException.Message), Is.EqualTo(GetInvariantMessageFragment(comparableInterpreterException.Message)));
    }

    public void AssertParityAndValue(string code, IEnumerable<string> modules, double expected)
    {
        var (compiler, interpreter) = ExecuteBoth(code, modules);
        AssertParity(compiler, interpreter);
        Assert.That(AsNumber(compiler), Is.EqualTo(expected).Within(1e-9));
    }

    private static string GetInvariantMessageFragment(string message)
    {
        const int maxLength = 64;
        var builder = new StringBuilder(message.Length);
        foreach (var c in message)
        {
            if (char.IsLetter(c))
            {
                builder.Append(char.ToLowerInvariant(c));
                continue;
            }

            if (!char.IsDigit(c))
                continue;

            if (builder.Length == 0 || builder[^1] != '#')
                builder.Append('#');
        }

        var normalized = builder.ToString().Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static Exception GetComparableException(Exception exception)
    {
        var current = exception;
        while (true)
        {
            if (current is AggregateException aggregateException && aggregateException.InnerExceptions.Count == 1)
            {
                current = aggregateException.InnerExceptions[0];
                continue;
            }

            if (current.InnerException == null)
                return current;

            current = current.InnerException;
        }
    }
}