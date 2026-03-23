using SemicolonAsNewLineModule;
using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace SemicolonAsNewLineModule;

public sealed class SemicolonAsNewLineDialectRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
{
    public decimal Order => 230m;

    public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        if (builder == null)
        {
            Thrower.ArgumentNull(nameof(builder));
        }

        var assembly = typeof(SemicolonAsNewLineModuleImpl).Assembly;
        builder.RegisterAttributedModulesFromAssemblies(assembly);
    }
}
