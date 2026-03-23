using LocalVariablesOptimizerModule;
using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace LocalVariablesOptimizerModule;

public sealed class LocalVariablesOptimizerDialectRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
{
    public int Order => 600;

    public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        if (builder == null)
        {
            Thrower.ArgumentNull(nameof(builder));
        }

        var assembly = typeof(LocalVariablesOptimizer).Assembly;
        builder.RegisterAttributedOptimizersFromAssemblies(assembly);
    }
}
