using IdentifierModule;
using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace IdentifierModule;

public sealed class IdentifierDialectRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
{
    public int Order => 210;

    public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        if (builder == null)
        {
            Thrower.ArgumentNull(nameof(builder));
        }

        var assembly = typeof(IdentifierModuleImpl).Assembly;
        builder.RegisterAttributedModulesFromAssemblies(assembly);
    }
}
