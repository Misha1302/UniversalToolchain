using CommentsModule;
using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace CommentsModule;

public sealed class CommentsDialectRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
{
    public int Order => 200;

    public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        if (builder == null)
        {
            Thrower.ArgumentNull(nameof(builder));
        }

        var assembly = typeof(CommentsModuleImpl).Assembly;
        builder.RegisterAttributedModulesFromAssemblies(assembly);
    }
}
