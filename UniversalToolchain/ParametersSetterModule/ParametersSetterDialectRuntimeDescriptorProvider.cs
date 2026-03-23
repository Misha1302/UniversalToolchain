using ParametersSetterModule;
using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace ParametersSetterModule;

public sealed class ParametersSetterDialectRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
{
    public decimal Order => 300m;

    public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        if (builder == null)
        {
            Thrower.ArgumentNull(nameof(builder));
        }

        var assembly = typeof(ParametersSetterModuleImpl).Assembly;
        builder.RegisterAttributedModulesFromAssemblies(assembly);
    }
}
