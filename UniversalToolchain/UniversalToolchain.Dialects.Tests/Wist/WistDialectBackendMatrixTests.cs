using System.Globalization;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using NumbersModule.Core;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests.Wist;

public sealed class WistDialectBackendMatrixTests
{
    [TestCaseSource(nameof(GetExamplePrograms))]
    public void ExampleProgram_BackendMatrix_ShouldMatchExpectedOutcomes(BackendMatrixCase @case)
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        var composition = workflow.ComposeFile(Path.Combine(@case.ExampleDirectory, "dialect.wistdialect"));
        Assert.That(composition.IsSuccess, Is.True, DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition)));

        using var host = workflow.CreateHost(composition);
        var code = File.ReadAllText(Path.Combine(@case.ExampleDirectory, "program.wist"));

        var interpreter = Execute(host, code, "interpreter");
        var compiler = Execute(host, code, "compiler");

        AssertBackendOutcome(interpreter, @case.InterpreterExpected, "interpreter");
        AssertBackendOutcome(compiler, @case.CompilerExpected, "compiler");

        if (@case.InterpreterExpected.IsSuccess && @case.CompilerExpected.IsSuccess)
            Assert.That(interpreter.ValueText, Is.EqualTo(compiler.ValueText), "Backend result parity failed.");

        if (@case.InterpreterExpected.IsFailure && @case.CompilerExpected.IsFailure)
            Assert.That(interpreter.ErrorMarker, Is.EqualTo(compiler.ErrorMarker), "Backend diagnostics parity failed.");
    }

    public static IEnumerable<TestCaseData> GetExamplePrograms()
    {
        yield return BuildCase(
            "minimal-arithmetic",
            ExpectedOutcome.Success("14"),
            ExpectedOutcome.Failure("Unknown execution mode 'compiler'"));

        yield return BuildCase(
            "full-default",
            ExpectedOutcome.Success("15"),
            ExpectedOutcome.Success("15"));

        yield return BuildCase(
            "full-default-native",
            ExpectedOutcome.Success("15"),
            ExpectedOutcome.Success("15"));
    }

    private static TestCaseData BuildCase(
        string exampleName,
        ExpectedOutcome interpreterExpected,
        ExpectedOutcome compilerExpected)
    {
        var directory = ResolveExampleDirectory(exampleName);
        return new TestCaseData(new BackendMatrixCase(directory, interpreterExpected, compilerExpected))
            .SetName($"Wist example matrix: {exampleName}");
    }

    private static ExecutionOutcome Execute(WistDialectExecutionHost host, string code, string backend)
    {
        try
        {
            var result = host.Run(code, backend);
            return ExecutionOutcome.Success(Normalize(result));
        }
        catch (Exception ex)
        {
            var marker = ex.Message;
            return ExecutionOutcome.Failure(marker);
        }
    }

    private static void AssertBackendOutcome(ExecutionOutcome actual, ExpectedOutcome expected, string backend)
    {
        if (expected.IsSuccess)
        {
            Assert.Multiple(() =>
            {
                Assert.That(actual.IsSuccess, Is.True, $"Expected backend '{backend}' success, but it failed with: {actual.ErrorMarker}");
                Assert.That(actual.ValueText, Is.EqualTo(expected.ExpectedValueText), $"Unexpected value for backend '{backend}'.");
            });

            return;
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual.IsSuccess, Is.False, $"Expected backend '{backend}' failure, but it succeeded with: {actual.ValueText}");
            Assert.That(actual.ErrorMarker, Does.Contain(expected.ExpectedDiagnosticMarker!), $"Unexpected diagnostics for backend '{backend}'.");
        });
    }

    private static string ResolveExampleDirectory(string name)
    {
        var path = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "Dialects", "examples", "wist", name));
        if (!Directory.Exists(path))
            Thrower.FileNotFound(path);

        return path;
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();
        return services.BuildServiceProvider();
    }

    private static string Normalize(object? value)
    {
        return value switch
        {
            RealNumberImpl number => number.GetValue().ToString(CultureInfo.InvariantCulture),
            int intValue => intValue.ToString(CultureInfo.InvariantCulture),
            long longValue => longValue.ToString(CultureInfo.InvariantCulture),
            double doubleValue => doubleValue.ToString("G17", CultureInfo.InvariantCulture),
            float floatValue => floatValue.ToString("G9", CultureInfo.InvariantCulture),
            decimal decimalValue => decimalValue.ToString(CultureInfo.InvariantCulture),
            null => "<null>",
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? value.ToString() ?? "<unknown>"
        };
    }

    public sealed record BackendMatrixCase(
        string ExampleDirectory,
        ExpectedOutcome InterpreterExpected,
        ExpectedOutcome CompilerExpected);

    public sealed record ExpectedOutcome(bool IsSuccess, string? ExpectedValueText, string? ExpectedDiagnosticMarker)
    {
        public bool IsFailure => !IsSuccess;

        public static ExpectedOutcome Success(string expectedValueText) => new(true, expectedValueText, null);

        public static ExpectedOutcome Failure(string expectedDiagnosticMarker) => new(false, null, expectedDiagnosticMarker);
    }

    private sealed record ExecutionOutcome(bool IsSuccess, string? ValueText, string? ErrorMarker)
    {
        public static ExecutionOutcome Success(string valueText) => new(true, valueText, null);

        public static ExecutionOutcome Failure(string errorMarker) => new(false, null, errorMarker);
    }
}
