namespace UniversalToolchain.Dialects.Frontend;

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