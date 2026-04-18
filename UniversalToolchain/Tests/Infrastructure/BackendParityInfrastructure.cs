using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Tests;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Modules.Tests;

/// <summary>
///     Unified parity infrastructure for compiler and interpreter backend execution.
/// </summary>
public static class BackendParityInfrastructure
{
    /// <summary>
    ///     Executes the same code in both backends and captures success/failure outcomes.
    /// </summary>
    public static (BackendExecutionResult CompilerResult, BackendExecutionResult InterpreterResult) RunBoth(string dialectText, string code)
    {
        using var compilerHost = CreateHost(EnsureSingleBackend(dialectText, "compiler"));
        using var interpreterHost = CreateHost(EnsureSingleBackend(dialectText, "interpreter"));

        var compilerResult = ExecuteSafely(() => compilerHost.Run(code, "compiler"));
        var interpreterResult = ExecuteSafely(() => interpreterHost.Run(code, "interpreter"));
        return (compilerResult, interpreterResult);
    }

    /// <summary>
    ///     Asserts that two backend outcomes are semantically equivalent.
    /// </summary>
    public static void AssertSemanticParity(BackendExecutionResult compilerResult, BackendExecutionResult interpreterResult)
    {
        Assert.That(compilerResult.IsSuccess, Is.EqualTo(interpreterResult.IsSuccess), "Backends must either both succeed or both fail.");

        if (compilerResult.IsSuccess)
        {
            BackendResultAssertions.AssertEquivalent(compilerResult.Value, interpreterResult.Value);
            return;
        }

        var compilerException = compilerResult.Exception ?? Thrower.InvalidOpEx<Exception>("Compiler result must contain exception on failure.");
        var interpreterException = interpreterResult.Exception ?? Thrower.InvalidOpEx<Exception>("Interpreter result must contain exception on failure.");

        Assert.That(compilerException.GetType().FullName, Is.EqualTo(interpreterException.GetType().FullName));
        Assert.That(compilerException.Message, Is.EqualTo(interpreterException.Message));
    }

    public static double AsNumber(object? value) => BackendResultAssertions.AsNumber(value);

    public static bool AsBool(object? value) => BackendResultAssertions.AsBool(value);

    /// <summary>
    ///     Executes a backend function and captures success/failure in <see cref="BackendExecutionResult" />.
    /// </summary>
    public static BackendExecutionResult ExecuteSafely(Func<object?> action)
    {
        try
        {
            return BackendExecutionResult.Success(action());
        }
        catch (Exception ex)
        {
            return BackendExecutionResult.Failure(ex);
        }
    }

    private static WistDialectExecutionHost CreateHost(string dialectText)
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(dialectText, "backend-parity-inline");
        if (!composition.IsSuccess)
            Thrower.InvalidOpEx(composition.ToDeterministicText());

        return workflow.CreateHost(composition);
    }

    private static string EnsureSingleBackend(string dialectText, string backend)
    {
        var lines = dialectText
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static line => !line.StartsWith("backend ", StringComparison.Ordinal))
            .ToList();

        lines.Add($"backend {backend}");
        return string.Join(Environment.NewLine, lines);
    }
}

/// <summary>
///     Captures backend execution outcome for parity checks in both success and failure scenarios.
/// </summary>
public sealed record BackendExecutionResult(bool IsSuccess, object? Value, Exception? Exception)
{
    public static BackendExecutionResult Success(object? value) => new(true, value, null);

    public static BackendExecutionResult Failure(Exception exception) => new(false, null, exception);
}