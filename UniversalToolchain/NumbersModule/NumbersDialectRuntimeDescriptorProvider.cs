using NumbersModule.Module;
using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace NumbersModule;

public sealed class NumbersDialectRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
{
    public int Order => 140;

    public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        if (builder == null)
        {
            Thrower.ArgumentNull(nameof(builder));
        }

        var assembly = typeof(NumbersModuleImpl).Assembly;
        builder.RegisterAttributedModulesFromAssemblies(assembly);
    }
}
