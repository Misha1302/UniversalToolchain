using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Testing.Infrastructure;

/// <summary>
///     Provides deterministic helpers for creating dialect hosts in tests.
/// </summary>
public static class DialectTestHostInfrastructure
{
    /// <summary>
    ///     Creates an execution host from inline dialect text through the canonical runtime path.
    /// </summary>
    public static WistDialectExecutionHost CreateHostFromDialectText(string dialectText)
    {
        if (string.IsNullOrWhiteSpace(dialectText))
            Thrower.Argument(nameof(dialectText), "Dialect text must not be empty.");

        var services = new ServiceCollection();
        services.AddWistDialectServices();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(dialectText, "tests-inline");
        if (!composition.IsSuccess)
            Thrower.InvalidOpEx(FormatComposition(composition));

        return workflow.CreateHost(composition);
    }

    /// <summary>
    ///     Creates a host by composing the original dialect through the canonical pipeline,
    ///     then applying a structured test-only backend override to the build plan.
    /// </summary>
    public static WistDialectExecutionHost CreateInterpreterHost(
        string dialectText,
        IReadOnlyCollection<System.Reflection.Assembly>? allowedAssemblies = null) =>
        CreateHostWithOnlyBackend(dialectText, "interpreter", allowedAssemblies);

    /// <summary>
    ///     Creates a host by composing the original dialect through the canonical pipeline,
    ///     then applying a structured test-only backend override to the build plan.
    /// </summary>
    public static WistDialectExecutionHost CreateCompilerHost(
        string dialectText,
        IReadOnlyCollection<System.Reflection.Assembly>? allowedAssemblies = null) =>
        CreateHostWithOnlyBackend(dialectText, "compiler", allowedAssemblies);

    /// <summary>
    ///     Executes code in both backends and verifies semantic parity.
    /// </summary>
    public static object? RunInBothBackends(string dialectText, string code)
    {
        var (compilerResult, interpreterResult) = BackendParityInfrastructure.RunBoth(dialectText, code);
        BackendParityInfrastructure.AssertSemanticParity(compilerResult, interpreterResult);
        return compilerResult.Value;
    }

    private static WistDialectExecutionHost CreateHostWithOnlyBackend(
        string dialectText,
        string backend,
        IReadOnlyCollection<System.Reflection.Assembly>? allowedAssemblies)
    {
        if (string.IsNullOrWhiteSpace(dialectText))
            Thrower.Argument(nameof(dialectText), "Dialect text must not be empty.");

        var services = new ServiceCollection();
        services.AddWistDialectServices();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var resolver = provider.GetRequiredService<SelectedRuntimePlanResolver>();
        var composition = workflow.ComposeText(dialectText, $"tests-inline-{backend}");
        if (!composition.IsSuccess)
            Thrower.InvalidOpEx(FormatComposition(composition));

        var overriddenComposition = DialectCompositionTestOverrides.WithOnlyBackend(composition, resolver, backend);
        if (!overriddenComposition.IsSuccess)
            Thrower.InvalidOpEx(FormatComposition(overriddenComposition));

        return workflow.CreateHost(
            overriddenComposition,
            new WistRuntimeServiceOptions
            {
                AllowedAssemblies = allowedAssemblies ?? Array.Empty<System.Reflection.Assembly>()
            });
    }

    private static string FormatComposition(DialectFrameworkCompositionResult composition) =>
        DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition));
}
