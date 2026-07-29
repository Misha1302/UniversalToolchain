using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests.Wist;

internal static class WistDialectTestInfrastructure
{
    public static ServiceProvider CreateCanonicalProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        return services.BuildServiceProvider();
    }

    /// <summary>
    ///     Creates a provider for tests that explicitly validate manual built-in backend registration behavior.
    ///     Canonical runtime-path tests should use <see cref="CreateCanonicalProvider" /> instead.
    /// </summary>
    public static ServiceProvider CreateProviderWithExplicitBackends()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();
        return services.BuildServiceProvider();
    }

    public static string BuildSelectionSignature(DialectFrameworkCompositionResult composition)
    {
        var selection = composition.RuntimeSelection as SelectedRuntimePlan;
        if (selection == null)
            return "<no-selection>";

        return string.Join("|", selection.OrderedModules.Select(static x => x.CanonicalAlias))
               + "::"
               + string.Join("|", selection.EnabledOptimizers.Select(static x => x.CanonicalAlias))
               + "::"
               + string.Join("|", selection.EnabledBackends.Select(static x => x.CanonicalAlias));
    }

    public static string BuildSelectionAndDiagnosticsSignature(DialectFrameworkCompositionResult composition)
    {
        var diagnostics = composition.SemanticDiagnostics
            .Concat(composition.ResolutionDiagnostics)
            .Select(static x => $"{x.Code}:{x.Severity}:{x.Message}");

        return BuildSelectionSignature(composition)
               + "::diagnostics::"
               + string.Join("|", diagnostics);
    }

    public static string BuildHostSignature(WistDialectExecutionHost host) => BuildConfigurationSignature(host.Configuration);

    public static string BuildConfigurationSignature(ToolchainRuntimeConfiguration configuration)
    {
        return string.Join("|", configuration.FrontendModules.Select(static x => x.FullName))
               + "::"
               + string.Join("|", configuration.IrModules.Select(static x => x.FullName))
               + "::"
               + string.Join("|", configuration.Optimizers.Select(static x => x.FullName))
               + "::"
               + string.Join("|", configuration.BackendConfigurations.Select(static x => x.BackendDescriptor.CanonicalId));
    }
}