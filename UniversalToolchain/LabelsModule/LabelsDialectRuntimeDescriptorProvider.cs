using LabelsModule.Module;
using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace LabelsModule;

public sealed class LabelsDialectRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
{
    public int Order => 400;

    public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        if (builder == null)
        {
            Thrower.ArgumentNull(nameof(builder));
        }

        var assembly = typeof(LabelsModuleImpl).Assembly;
        builder.RegisterAttributedModulesFromAssemblies(assembly);
    }
}
