using CSharpInteropModule.Module;
using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace CSharpInteropModule;

public sealed class CSharpInteropDialectRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
{
    public decimal Order => 500m;

    public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        if (builder == null)
        {
            Thrower.ArgumentNull(nameof(builder));
        }

        var assembly = typeof(CSharpInteropModuleImpl).Assembly;
        builder.RegisterAttributedModulesFromAssemblies(assembly);
    }
}
