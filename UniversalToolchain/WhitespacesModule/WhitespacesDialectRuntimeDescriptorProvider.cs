using WhitespacesModule;
using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace WhitespacesModule;

public sealed class WhitespacesDialectRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
{
    public int Order => 240;

    public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        if (builder == null)
        {
            Thrower.ArgumentNull(nameof(builder));
        }

        var assembly = typeof(WhitespaceModuleImpl).Assembly;
        builder.RegisterAttributedModulesFromAssemblies(assembly);
    }
}
