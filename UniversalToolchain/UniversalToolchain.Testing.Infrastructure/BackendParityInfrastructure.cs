using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Testing.Infrastructure;

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
        using var provider = CreateCanonicalProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var resolver = provider.GetRequiredService<SelectedRuntimePlanResolver>();
        var baseComposition = workflow.ComposeText(dialectText, "backend-parity-inline");
        if (!baseComposition.IsSuccess)
            Thrower.InvalidOpEx(FormatComposition(baseComposition));

        var compilerComposition = DialectCompositionTestOverrides.WithOnlyBackend(baseComposition, resolver, "cil");
        var interpreterComposition = DialectCompositionTestOverrides.WithOnlyBackend(baseComposition, resolver, "interpreter");
        if (!compilerComposition.IsSuccess)
            Thrower.InvalidOpEx(FormatComposition(compilerComposition));
        if (!interpreterComposition.IsSuccess)
            Thrower.InvalidOpEx(FormatComposition(interpreterComposition));

        using var compilerHost = workflow.CreateHost(compilerComposition);
        using var interpreterHost = workflow.CreateHost(interpreterComposition);

        var compilerResult = ExecuteHostSafely(compilerHost, code, "cil");
        var interpreterResult = ExecuteHostSafely(interpreterHost, code, "interpreter");
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

    private static BackendExecutionResult ExecuteHostSafely(WistDialectExecutionHost host, string code, string backendName)
    {
        try
        {
            return BackendExecutionResult.Success(host.Run(code, backendName));
        }
        catch (Exception ex)
        {
            return BackendExecutionResult.Failure(ex);
        }
    }

    private static ServiceProvider CreateCanonicalProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        return services.BuildServiceProvider();
    }

    private static string FormatComposition(DialectFrameworkCompositionResult composition) =>
        DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition));
}

/// <summary>
///     Captures backend execution outcome for parity checks in both success and failure scenarios.
/// </summary>
public sealed record BackendExecutionResult(bool IsSuccess, object? Value, Exception? Exception)
{
    public static BackendExecutionResult Success(object? value) => new(true, value, null);

    public static BackendExecutionResult Failure(Exception exception) => new(false, null, exception);
}