using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Modules.Tests;

namespace UniversalToolchain.Dialects.Tests;

/// <summary>
///     Provides deterministic helpers for creating dialect hosts in tests.
/// </summary>
public static class DialectTestHostInfrastructure
{
    /// <summary>
    ///     Creates an execution host from inline dialect text.
    /// </summary>
    public static WistDialectExecutionHost CreateHostFromDialectText(string dialectText)
    {
        if (string.IsNullOrWhiteSpace(dialectText))
            Thrower.Argument(nameof(dialectText), "Dialect text must not be empty.");

        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(dialectText, "tests-inline");
        if (!composition.IsSuccess)
            Thrower.InvalidOpEx(DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition)));

        return workflow.CreateHost(composition);
    }

    /// <summary>
    ///     Creates a host configured only for the interpreter backend.
    /// </summary>
    public static WistDialectExecutionHost CreateInterpreterHost(string dialectText) => CreateHostFromDialectText(EnsureSingleBackend(dialectText, "interpreter"));

    /// <summary>
    ///     Creates a host configured only for the compiler backend.
    /// </summary>
    public static WistDialectExecutionHost CreateCompilerHost(string dialectText) => CreateHostFromDialectText(EnsureSingleBackend(dialectText, "compiler"));

    /// <summary>
    ///     Executes code in both backends and verifies semantic parity.
    /// </summary>
    public static object? RunInBothBackends(string dialectText, string code)
    {
        var (compilerResult, interpreterResult) = BackendParityInfrastructure.RunBoth(dialectText, code);
        BackendParityInfrastructure.AssertSemanticParity(compilerResult, interpreterResult);
        return compilerResult.Value;
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