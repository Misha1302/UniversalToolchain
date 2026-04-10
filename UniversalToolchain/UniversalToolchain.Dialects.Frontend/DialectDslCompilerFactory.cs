using ExceptionsManager;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDslCompilerFactory : IDialectDslCompilerFactory
{
    private readonly DialectDslFrontendModule _frontendModule;

    public DialectDslCompilerFactory(DialectDslFrontendModule frontendModule)
    {
        if (frontendModule == null)
            Thrower.ArgumentNull(nameof(frontendModule));

        _frontendModule = frontendModule;
    }

    public DialectDslCompiler Create()
    {
        return new DialectDslCompiler(_frontendModule);
    }
}
