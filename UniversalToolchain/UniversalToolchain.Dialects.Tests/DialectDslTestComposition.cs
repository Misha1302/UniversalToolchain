using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Frontend;

namespace UniversalToolchain.Dialects.Tests;

internal static class DialectDslTestComposition
{
    public static ServiceProvider CreateProvider(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddDialectDsl();
        services.AddDialectDslBuiltIns();
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
        return provider.GetRequiredService<DialectDslCompiler>();
    }

    public static DialectDslFrontendModule CreateFrontendModule(Action<IServiceCollection>? configure = null)
    {
        using var provider = CreateProvider(configure);
        return provider.GetRequiredService<DialectDslFrontendModule>();
    }
}
