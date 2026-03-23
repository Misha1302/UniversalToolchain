using ScopesModule.Module;
using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace ScopesModule;

public sealed class ScopesDialectRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
{
    public int Order => 310;

    public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        if (builder == null)
        {
            Thrower.ArgumentNull(nameof(builder));
        }

        var assembly = typeof(ScopesModuleImpl).Assembly;
        builder.RegisterAttributedModulesFromAssemblies(assembly);
    }
}
