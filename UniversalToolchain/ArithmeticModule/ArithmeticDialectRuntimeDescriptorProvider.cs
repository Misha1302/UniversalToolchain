using ArithmeticModule.Module;
using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace ArithmeticModule;

public sealed class ArithmeticDialectRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
{
    public decimal Order => 100m;

    public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        if (builder == null)
        {
            Thrower.ArgumentNull(nameof(builder));
        }

        var assembly = typeof(ArithmeticModuleImpl).Assembly;
        builder.RegisterAttributedModulesFromAssemblies(assembly);
    }
}
