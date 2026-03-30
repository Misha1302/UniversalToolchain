using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Wist;

namespace Tests.Infrastructure;

/// <summary>
/// Provides deterministic helpers for creating dialect hosts in tests.
/// </summary>
public static class DialectTestHostInfrastructure
{
    /// <summary>
    /// Creates an execution host from inline dialect text.
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
            Thrower.InvalidOpEx(composition.ToDeterministicText());

        return workflow.CreateHost(composition);
    }

    /// <summary>
    /// Creates a host configured only for the interpreter backend.
    /// </summary>
    public static WistDialectExecutionHost CreateInterpreterHost(string dialectText)
    {
        return CreateHostFromDialectText(EnsureSingleBackend(dialectText, "interpreter"));
    }

    /// <summary>
    /// Creates a host configured only for the compiler backend.
    /// </summary>
    public static WistDialectExecutionHost CreateCompilerHost(string dialectText)
    {
        return CreateHostFromDialectText(EnsureSingleBackend(dialectText, "compiler"));
    }

    /// <summary>
    /// Executes code in both backends and verifies semantic parity.
    /// </summary>
    public static object? RunInBothBackends(string dialectText, string code)
    {
        using var compilerHost = CreateCompilerHost(dialectText);
        using var interpreterHost = CreateInterpreterHost(dialectText);

        var compilerResult = compilerHost.Run(code, "compiler");
        var interpreterResult = interpreterHost.Run(code, "interpreter");

        Assert.That(interpreterResult, Is.EqualTo(compilerResult), "Interpreter and compiler results must match.");
        return compilerResult;
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
