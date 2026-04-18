using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDslCompilerFactory : IDialectDslCompilerFactory
{
    private readonly DialectDslFrontendModule _frontendModule;

    public DialectDslCompilerFactory(DialectDslFrontendModule frontendModule)
    {
        frontendModule = frontendModule.ArgNotNull();

        _frontendModule = frontendModule;
    }

    public DialectDslCompiler Create() => new(_frontendModule);
}