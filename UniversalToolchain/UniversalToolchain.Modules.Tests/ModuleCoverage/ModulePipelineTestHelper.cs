using System.Text;
using UniversalToolchain.Testing.Infrastructure;
using UniversalToolchain.Wist;

namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

internal sealed class ModulePipelineTestHelper : IDisposable
{
    public static readonly string[] FullUniversalModules =
    [
        "Whitespaces", "SemicolonAsNewLine", "Comments", "Numbers", "Identifier", "Arithmetic", "Equality",
        "Conditions", "ComparisonConditions", "BooleanConditions", "Loops", "Scopes", "Variables", "Labels",
        "InternalPreprocessorLexemes", "CSharpInterop"
    ];

    public void Dispose()
    {
    }

    public string BuildDialectText(
        string name,
        IEnumerable<string> modules,
        IEnumerable<string>? optimizers = null,
        IEnumerable<string>? backends = null)
    {
        var moduleList = Materialize(modules);
        var optimizerList = optimizers == null ? null : Materialize(optimizers);
        var backendList = backends == null ? ["cil", "interpreter"] : Materialize(backends);

        var modulesLine = string.Join(',', moduleList);
        var optimizerLine = optimizerList == null ? string.Empty : $"\nenable {string.Join(',', optimizerList)}";
        var backendLine = $"\nbackend {string.Join(',', backendList)}";
        var securityLines = moduleList.Contains("CSharpInterop", StringComparer.Ordinal)
            ? "\nsecurity trusted\ncapability unsafe-interop"
            : "\nsecurity restricted";
        return $"dialect {name}\nuse {modulesLine}{optimizerLine}{backendLine}{securityLines}";
    }

    public object? Execute(
        string code,
        string mode,
        IEnumerable<string> modules,
        IEnumerable<string>? optimizers = null)
    {
        var moduleList = Materialize(modules);
        var dialectText = BuildDialectText("Inline", moduleList, optimizers, [mode]);
        var options = WistEngineOptions.FromDialectText(dialectText, "module-pipeline-inline");
        options.BackendId = mode;
        if (moduleList.Contains("CSharpInterop", StringComparer.Ordinal))
        {
            options.AllowedAssemblies =
            [
                typeof(int).Assembly,
                typeof(ModulePipelineTestHelper).Assembly
            ];
        }

        using var engine = WistEngine.Create(options);
        return engine.Evaluate<object?>(code);
    }

    public object? ExecuteCompiler(string code, IEnumerable<string> modules, IEnumerable<string>? optimizers = null)
        => Execute(code, "cil", modules, optimizers);

    public object? ExecuteInterpreter(string code, IEnumerable<string> modules, IEnumerable<string>? optimizers = null)
        => Execute(code, "interpreter", modules, optimizers);

    public (object? Compiler, object? Interpreter) ExecuteBoth(
        string code,
        IEnumerable<string> modules,
        IEnumerable<string>? optimizers = null)
    {
        var moduleList = Materialize(modules);
        var optimizerList = optimizers == null ? null : Materialize(optimizers);
        var interpreter = Execute(code, "interpreter", moduleList, optimizerList);
        var compiler = Execute(code, "cil", moduleList, optimizerList);
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

    public void ExecuteEquivalent(
        string a,
        string b,
        IEnumerable<string> modules,
        IEnumerable<string>? optimizers = null)
    {
        var moduleList = Materialize(modules);
        var optimizerList = optimizers == null ? null : Materialize(optimizers);
        var resultA = ExecuteBoth(a, moduleList, optimizerList);
        var resultB = ExecuteBoth(b, moduleList, optimizerList);
        AssertParity(resultA.Compiler, resultA.Interpreter);
        AssertParity(resultB.Compiler, resultB.Interpreter);
        AssertSemanticEqual(resultA.Compiler, resultB.Compiler);
        AssertSemanticEqual(resultA.Interpreter, resultB.Interpreter);
    }

    public void ExecuteDifferent(
        string a,
        string b,
        IEnumerable<string> modules,
        IEnumerable<string>? optimizers = null)
    {
        var moduleList = Materialize(modules);
        var optimizerList = optimizers == null ? null : Materialize(optimizers);
        var resultA = ExecuteBoth(a, moduleList, optimizerList);
        var resultB = ExecuteBoth(b, moduleList, optimizerList);
        AssertParity(resultA.Compiler, resultA.Interpreter);
        AssertParity(resultB.Compiler, resultB.Interpreter);
        AssertSemanticNotEqual(resultA.Compiler, resultB.Compiler);
        AssertSemanticNotEqual(resultA.Interpreter, resultB.Interpreter);
    }

    public void AssertFailsContaining(string code, IEnumerable<string> modules, string expectedFragment)
    {
        var moduleList = Materialize(modules);
        var dialectText = BuildDialectText("Inline", moduleList, null, ["cil", "interpreter"]);
        var (compilerResult, interpreterResult) = RunBoth(dialectText, code, moduleList);

        Assert.That(compilerResult.IsSuccess, Is.False);
        Assert.That(interpreterResult.IsSuccess, Is.False);

        var compilerExceptionText = GetComparableException(compilerResult.Exception!).ToString();
        var interpreterExceptionText = GetComparableException(interpreterResult.Exception!).ToString();
        Assert.That(
            compilerExceptionText.Contains(expectedFragment, StringComparison.OrdinalIgnoreCase),
            Is.True,
            $"Compiler failure did not contain '{expectedFragment}': {compilerExceptionText}");
        Assert.That(
            interpreterExceptionText.Contains(expectedFragment, StringComparison.OrdinalIgnoreCase),
            Is.True,
            $"Interpreter failure did not contain '{expectedFragment}': {interpreterExceptionText}");
    }

    public void AssertFails(string code, IEnumerable<string> modules)
    {
        var moduleList = Materialize(modules);
        var dialectText = BuildDialectText("Inline", moduleList, null, ["cil", "interpreter"]);
        var (compilerResult, interpreterResult) = RunBoth(dialectText, code, moduleList);

        Assert.That(compilerResult.IsSuccess, Is.False);
        Assert.That(interpreterResult.IsSuccess, Is.False);
    }

    public void AssertCompilerAndInterpreterFailSameWay(string code, IEnumerable<string> modules)
    {
        var moduleList = Materialize(modules);
        var dialectText = BuildDialectText("Inline", moduleList, null, ["cil", "interpreter"]);
        var (compilerResult, interpreterResult) = RunBoth(dialectText, code, moduleList);

        Assert.That(compilerResult.IsSuccess, Is.False);
        Assert.That(interpreterResult.IsSuccess, Is.False);

        var comparableCompilerException = GetComparableException(compilerResult.Exception!);
        var comparableInterpreterException = GetComparableException(interpreterResult.Exception!);
        Assert.That(comparableCompilerException.GetType(), Is.EqualTo(comparableInterpreterException.GetType()));
        Assert.That(
            GetInvariantMessageFragment(comparableCompilerException.Message),
            Is.EqualTo(GetInvariantMessageFragment(comparableInterpreterException.Message)));
    }

    public void AssertParityAndValue(string code, IEnumerable<string> modules, double expected)
    {
        var (compiler, interpreter) = ExecuteBoth(code, modules);
        AssertParity(compiler, interpreter);
        Assert.That(AsNumber(compiler), Is.EqualTo(expected).Within(1e-9));
    }

    private (BackendExecutionResult CompilerResult, BackendExecutionResult InterpreterResult) RunBoth(
        string dialectText,
        string code,
        IReadOnlyList<string> modules)
    {
        if (!modules.Contains("CSharpInterop", StringComparer.Ordinal))
            return BackendParityInfrastructure.RunBoth(dialectText, code);

        return (
            RunSafely(() => Execute(code, "cil", modules)),
            RunSafely(() => Execute(code, "interpreter", modules)));
    }

    private static BackendExecutionResult RunSafely(Func<object?> action) =>
        BackendParityInfrastructure.ExecuteSafely(action);

    private static IReadOnlyList<string> Materialize(IEnumerable<string> values)
        => values as IReadOnlyList<string> ?? values.ToArray();

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
