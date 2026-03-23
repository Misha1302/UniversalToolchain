using NativeMathModule;
using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace NativeMathModule;

public sealed class NativeMathDialectRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
{
    public int Order => 130;

    public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        if (builder == null)
        {
            Thrower.ArgumentNull(nameof(builder));
        }

        var assembly = typeof(NativeTypesModuleImpl).Assembly;

        builder
            .RegisterAttributedModulesFromAssemblies(assembly)
            .RegisterAttributedOptimizersFromAssemblies(assembly);
    }
}
