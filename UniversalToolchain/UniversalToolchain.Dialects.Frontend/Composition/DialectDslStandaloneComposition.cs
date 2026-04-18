using Microsoft.Extensions.DependencyInjection;

namespace UniversalToolchain.Dialects.Frontend.Composition;

internal static class DialectDslStandaloneComposition
{
    public static DialectDslFrontendModule CreateFrontendModule()
    {
        var services = new ServiceCollection();
        services.AddDialectDslDefaultComposition();

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<DialectDslFrontendModule>();
    }
}