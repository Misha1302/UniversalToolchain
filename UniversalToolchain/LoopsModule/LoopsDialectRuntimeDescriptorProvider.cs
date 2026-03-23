using LoopsModule.Module;
using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace LoopsModule;

public sealed class LoopsDialectRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
{
    public decimal Order => 410m;

    public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        if (builder == null)
        {
            Thrower.ArgumentNull(nameof(builder));
        }

        var assembly = typeof(LoopsModuleImpl).Assembly;
        builder.RegisterAttributedModulesFromAssemblies(assembly);
    }
}
