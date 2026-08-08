using UniversalToolchain.Wist;

namespace UniversalToolchain.Testing.Infrastructure;

/// <summary>
/// Unified parity infrastructure over the canonical Wist public facade.
/// </summary>
public static class BackendParityInfrastructure
{
    public static (BackendExecutionResult CompilerResult, BackendExecutionResult InterpreterResult) RunBoth(
        string dialectText,
        string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dialectText);
        ArgumentNullException.ThrowIfNull(code);

        var compilerResult = ExecuteSafely(() =>
        {
            using var engine = CreateEngine(dialectText, "cil");
            return engine.Evaluate<object?>(code);
        });
        var interpreterResult = ExecuteSafely(() =>
        {
            using var engine = CreateEngine(dialectText, "interpreter");
            return engine.Evaluate<object?>(code);
        });
        return (compilerResult, interpreterResult);
    }

    public static void AssertSemanticParity(
        BackendExecutionResult compilerResult,
        BackendExecutionResult interpreterResult)
    {
        Assert.That(compilerResult.IsSuccess, Is.EqualTo(interpreterResult.IsSuccess),
            "Backends must either both succeed or both fail.");

        if (compilerResult.IsSuccess)
        {
            BackendResultAssertions.AssertEquivalent(compilerResult.Value, interpreterResult.Value);
            return;
        }

        var compilerException = compilerResult.Exception
            ?? throw new InvalidOperationException("Compiler result must contain exception on failure.");
        var interpreterException = interpreterResult.Exception
            ?? throw new InvalidOperationException("Interpreter result must contain exception on failure.");
        Assert.That(compilerException.GetType().FullName, Is.EqualTo(interpreterException.GetType().FullName));
        Assert.That(compilerException.Message, Is.EqualTo(interpreterException.Message));
    }

    public static double AsNumber(object? value) => BackendResultAssertions.AsNumber(value);
    public static bool AsBool(object? value) => BackendResultAssertions.AsBool(value);

    public static BackendExecutionResult ExecuteSafely(Func<object?> action)
    {
        try
        {
            return BackendExecutionResult.Success(action());
        }
        catch (Exception exception)
        {
            return BackendExecutionResult.Failure(exception);
        }
    }

    private static WistEngine CreateEngine(string dialectText, string backend)
    {
        var options = WistEngineOptions.FromDialectText(dialectText, "backend-parity-inline");
        options.BackendId = backend;
        return WistEngine.Create(options);
    }
}

public sealed record BackendExecutionResult(bool IsSuccess, object? Value, Exception? Exception)
{
    public static BackendExecutionResult Success(object? value) => new(true, value, null);
    public static BackendExecutionResult Failure(Exception exception) => new(false, null, exception);
}
