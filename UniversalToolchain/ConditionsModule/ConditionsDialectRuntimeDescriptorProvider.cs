using ConditionsModule.Module;
using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace ConditionsModule;

public sealed class ConditionsDialectRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
{
    public decimal Order => 110m;

    public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        if (builder == null)
        {
            Thrower.ArgumentNull(nameof(builder));
        }

        var assembly = typeof(ConditionsModuleImpl).Assembly;

        builder
            .RegisterAttributedModulesFromAssemblies(assembly)
            .RegisterAttributedOptimizersFromAssemblies(assembly);
    }
}
