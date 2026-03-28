using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests.Wist;

internal static class WistDialectTestInfrastructure
{
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

    public static string BuildHostSignature(WistDialectExecutionHost host)
    {
        return string.Join("|", host.Configuration.FrontendModules.Select(static x => x.FullName))
               + "::"
               + string.Join("|", host.Configuration.IrModules.Select(static x => x.FullName))
               + "::"
               + string.Join("|", host.Configuration.Optimizers.Select(static x => x.FullName))
               + "::"
               + string.Join("|", host.Configuration.BackendConfigurations.Select(static x => x.BackendDescriptor.CanonicalId));
    }
}