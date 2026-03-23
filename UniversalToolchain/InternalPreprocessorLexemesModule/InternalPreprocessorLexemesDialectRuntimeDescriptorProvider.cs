using InternalPreprocessorLexemesModule;
using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;

namespace InternalPreprocessorLexemesModule;

public sealed class InternalPreprocessorLexemesDialectRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
{
    public decimal Order => 220m;

    public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        if (builder == null)
        {
            Thrower.ArgumentNull(nameof(builder));
        }

        var assembly = typeof(InternalPreprocessorLexemesModuleImpl).Assembly;
        builder.RegisterAttributedModulesFromAssemblies(assembly);
    }
}
