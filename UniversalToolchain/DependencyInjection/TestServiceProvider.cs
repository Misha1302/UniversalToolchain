using AbstractIrConverters;
using BasicCodeTranslator;
using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicLexer;
using BasicParser;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection;

/// <summary>
///     Helper class for managing ServiceProvider in test environments
/// </summary>
public static class TestServiceProvider
{
    /// <summary>
    ///     Builds a service provider with default Wist test configuration
    /// </summary>
    /// <param name="configureServices">Optional additional configuration</param>
    /// <returns>Configured service provider</returns>
    public static IServiceProvider BuildTestProvider(
        Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();

        // Add default Wist test services
        services.AddWistTestServices();
        services.AddCoreRunnables();

        // Allow additional configuration
        configureServices?.Invoke(services);

        return services.BuildServiceProvider();
    }

    /// <summary>
    ///     Builds a service provider with minimum required services for testing
    /// </summary>
    /// <param name="configureServices">Optional additional configuration</param>
    /// <returns>Configured service provider</returns>
    public static IServiceProvider BuildMinimalTestProvider(
        Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();

        // Add only essential services
        services.AddTransient<Func<ILexer>>(_ => () => new BasicLexerImpl());
        services.AddTransient<Func<IParser>>(_ => () => new BasicParserImpl());
        services.AddTransient<Func<IAstToBytecodeTranslator>>(_ => () => new BasicAstToBytecodeTranslatorImpl());
        services.AddTransient<Func<IAbstractMethodsTranslator>>(_ => () => new BytecodeToAbstractIrConverterImpl());

        // Allow additional configuration
        configureServices?.Invoke(services);

        return services.BuildServiceProvider();
    }

    /// <summary>
    ///     Creates a test provider with custom modules
    /// </summary>
    /// <param name="modules">Custom frontend modules to register</param>
    /// <returns>Configured service provider</returns>
    public static IServiceProvider BuildProviderWithModules(
        params IFrontendCoreModule[] modules)
    {
        return BuildTestProvider(services =>
        {
            // Clear default modules and add custom ones
            var serviceDescriptors = services
                .Where(s => s.ServiceType == typeof(IFrontendCoreModule))
                .ToList();

            foreach (var descriptor in serviceDescriptors)
            {
                services.Remove(descriptor);
            }

            foreach (var module in modules)
            {
                services.AddSingleton<IFrontendCoreModule>(module);
            }
        });
    }

    /// <summary>
    ///     Creates a test provider with mocked dependencies
    /// </summary>
    /// <typeparam name="TMockLexer">Mock lexer type</typeparam>
    /// <typeparam name="TMockParser">Mock parser type</typeparam>
    /// <returns>Configured service provider with mocks</returns>
    public static IServiceProvider BuildProviderWithMocks<TMockLexer, TMockParser>()
        where TMockLexer : class, ILexer
        where TMockParser : class, IParser
    {
        return BuildTestProvider(services =>
        {
            // Replace default implementations with mocks
            var lexerDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(Func<ILexer>));

            if (lexerDescriptor != null)
            {
                services.Remove(lexerDescriptor);
            }

            var parserDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(Func<IParser>));

            if (parserDescriptor != null)
            {
                services.Remove(parserDescriptor);
            }

            // Register mocks
            services.AddTransient<Func<ILexer>>(_ => () =>
                ActivatorUtilities.CreateInstance<TMockLexer>(_));
            services.AddTransient<Func<IParser>>(_ => () =>
                ActivatorUtilities.CreateInstance<TMockParser>(_));
        });
    }
}