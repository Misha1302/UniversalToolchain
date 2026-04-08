using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Frontend;

namespace UniversalToolchain.Dialects.Tests;

internal static class DialectDslTestComposition
{
    public static ServiceProvider CreateProvider(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddDialectDslDefaultComposition();
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    public static DialectDslRegistry CreateRegistry(Action<IServiceCollection>? configure = null)
    {
        using var provider = CreateProvider(configure);
        return provider.GetRequiredService<DialectDslRegistry>();
    }

    public static DialectDslCompiler CreateCompiler(Action<IServiceCollection>? configure = null)
    {
        using var provider = CreateProvider(configure);
        return new DialectDslCompiler(provider.GetRequiredService<DialectDslFrontendModule>());
    }

    public static DialectDslFrontendModule CreateFrontendModule(Action<IServiceCollection>? configure = null)
    {
        using var provider = CreateProvider(configure);
        return provider.GetRequiredService<DialectDslFrontendModule>();
    }
}
