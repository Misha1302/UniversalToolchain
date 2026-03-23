using ParametersSetterModule;
using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace ParametersSetterModule;

public sealed class ParametersSetterDialectRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
{
    public int Order => 300;

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
