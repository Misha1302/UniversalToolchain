namespace DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers all Wist services with automatic module discovery.
    /// </summary>
    public static IServiceCollection AddWistServices(
        this IServiceCollection services,
        string? servicesDirectory = null) =>
        services.AddWistServices(null, servicesDirectory);

    /// <summary>
    ///     Registers all Wist services with configurable options.
    /// </summary>
    public static IServiceCollection AddWistServices(
        this IServiceCollection services,
        Action<WistOptions>? configureOptions,
        string? servicesDirectory = null)
    {
        var options = new WistOptions();
        configureOptions?.Invoke(options);

        // Register core service factories.
        RegisterCoreFactories(services);

        // Automatically register all services marked with AutoRegisterService.
        RegisterAutoDiscoveredServices(services, servicesDirectory);

        // Apply filters and options.
        ApplyOptionsFilters(services, options);

        // Register compilers explicitly.
        RegisterCompilers(services);

        // Register runnable cores based on selected modules.
        RegisterCoreRunnables(services);

        return services;
    }

    /// <summary>
    ///     Registers the minimal service set required for core execution (without modules).
    /// </summary>
    public static IServiceCollection AddWistCoreServices(
        this IServiceCollection services)
    {
        // Core factories.
        services.AddTransient<Func<ILexer>>(_ => () => new BasicLexerImpl());
        services.AddTransient<Func<IParser>>(_ => () => new BasicParserImpl());
        services.AddTransient<Func<IAstToBytecodeTranslator>>(_ => () => new BasicAstToBytecodeTranslatorImpl());
        services.AddTransient<Func<IAbstractMethodsTranslator>>(_ => () => new BytecodeToAbstractIrConverterImpl());
        services.AddTransient<Func<IExecutor<DynamicMethod>>>(_ => () => new DynamicMethodExecutor());
        services.AddTransient<Func<IExecutor<IAbstractIR>>>(_ => () => new InterpreterImpl());

        // Compilers.
        services.AddTransient<AbstractMethodsCompilerImpl>();
        services.AddTransient<AbstractIrToAbstractIrStub>();

        return services;
    }

    /// <summary>
    ///     Explicitly adds a frontend module (without automatic discovery).
    /// </summary>
    public static IServiceCollection AddModule<TModule>(this IServiceCollection services)
        where TModule : class, IFrontendCoreModule
    {
        services.AddSingleton<IFrontendCoreModule, TModule>();
        return services;
    }

    /// <summary>
    ///     Explicitly adds an IR optimization module (without automatic discovery).
    /// </summary>
    public static IServiceCollection AddIrOptimizerModule<TOptimizer>(this IServiceCollection services)
        where TOptimizer : class, IIRProcessingModule
    {
        services.AddTransient<IIRProcessingModule, TOptimizer>();
        return services;
    }

    /// <summary>
    ///     Removes all services from the specified namespace.
    /// </summary>
    public static IServiceCollection RemoveAllByNamespace(
        this IServiceCollection services,
        string namespaceName)
    {
        var descriptors = services
            .Where(d => NamespaceMatches(d.ImplementationType?.Namespace, namespaceName) ||
                        NamespaceMatches(d.ServiceType.Namespace, namespaceName))
            .ToList();

        foreach (var descriptor in descriptors)
            services.Remove(descriptor);

        return services;
    }

    /// <summary>
    ///     Removes all services implementing the specified interface.
    /// </summary>
    public static IServiceCollection RemoveAllByServiceType<TService>(
        this IServiceCollection services)
        where TService : class
    {
        var descriptors = services
            .Where(d => d.ServiceType == typeof(TService))
            .ToList();

        foreach (var descriptor in descriptors)
            services.Remove(descriptor);

        return services;
    }

    private static void RegisterCoreFactories(
        IServiceCollection services)
    {
        // Lexer and parser.
        services.AddTransient<Func<ILexer>>(_ =>
        {
            var config = new LexerConfiguration([]);
            return () => new BasicLexerImpl(config);
        });

        services.AddTransient<Func<IParser>>(_ =>
        {
            var config = new ParserConfiguration([]);
            return () => new BasicParserImpl(config);
        });

        // Translators.
        services.AddTransient<Func<IAstToBytecodeTranslator>>(_ =>
            () => new BasicAstToBytecodeTranslatorImpl());

        services.AddTransient<Func<IAbstractMethodsTranslator>>(_ =>
            () => new BytecodeToAbstractIrConverterImpl());

        // Executors.
        services.AddTransient<Func<IExecutor<DynamicMethod>>>(_ =>
            () => new DynamicMethodExecutor());

        services.AddTransient<Func<IExecutor<IAbstractIR>>>(_ =>
            () => new InterpreterImpl());
    }

    private static void RegisterAutoDiscoveredServices(
        IServiceCollection services,
        string? servicesDirectory)
    {
        var assemblies = servicesDirectory != null
            ? TypesFinder.LoadAllAssemblies(Path.GetFullPath(servicesDirectory)).ToList()
            : TypesFinder.Assemblies;

        services.AddAutoRegisteredServices(assemblies);
    }

    private static void ApplyOptionsFilters(
        IServiceCollection services,
        WistOptions options)
    {
        // Apply exclusion filters.
        if (options.ExcludedNamespaces?.Any() == true)
            foreach (var ns in options.ExcludedNamespaces)
                services.RemoveAllByNamespace(ns);

        // Apply inclusion filters.
        if (options.IncludedNamespaces?.Any() == true)
        {
            // Find all registered modules.
            var allModules = services
                .Where(d => typeof(IFrontendCoreModule).IsAssignableFrom(d.ServiceType) ||
                            typeof(IIRProcessingModule).IsAssignableFrom(d.ServiceType))
                .Where(d => d.ImplementationType != null)
                .ToList();

            // Remove modules that are not in the inclusion list.
            foreach (var module in allModules)
            {
                if (module.ImplementationType?.Namespace == null ||
                    !options.IncludedNamespaces.Any(ns => NamespaceMatches(module.ImplementationType.Namespace, ns)))
                    services.Remove(module);
            }
        }

        // Process arithmetic modules according to options.
        switch (options.ArithmeticMode)
        {
            case WistOptions.ArithmeticModeEnum.None:
                services.RemoveAllByNamespace("ArithmeticModule");
                services.RemoveAllByNamespace("NativeMathModule");
                services.RemoveAllByNamespace("NumbersModule");
                break;

            case WistOptions.ArithmeticModeEnum.Universal:
                services.RemoveAllByNamespace("NativeMathModule");
                break;

            case WistOptions.ArithmeticModeEnum.Native:
                services.RemoveAllByNamespace("NumbersModule");
                services.RemoveAllByNamespace("ArithmeticModule");
                break;
        }

        // Remove modules explicitly marked for removal.
        if (options.ModulesToRemove?.Any() == true)
            foreach (var moduleType in options.ModulesToRemove)
            {
                var moduleTypes = services
                    .Where(d => d.ImplementationType == moduleType)
                    .ToList();

                foreach (var module in moduleTypes)
                    services.Remove(module);
            }
    }


    private static bool NamespaceMatches(string? candidateNamespace, string namespaceFilter)
    {
        if (string.IsNullOrWhiteSpace(candidateNamespace) || string.IsNullOrWhiteSpace(namespaceFilter))
            return false;

        return candidateNamespace.Equals(namespaceFilter, StringComparison.Ordinal) ||
               candidateNamespace.StartsWith($"{namespaceFilter}.", StringComparison.Ordinal) ||
               candidateNamespace.EndsWith($".{namespaceFilter}", StringComparison.Ordinal) ||
               candidateNamespace.Contains($".{namespaceFilter}.", StringComparison.Ordinal);
    }

    private static void RegisterCompilers(IServiceCollection services)
    {
        services.AddTransient<AbstractMethodsCompilerImpl>();
        services.AddTransient<AbstractIrToAbstractIrStub>();
    }

    private static void RegisterCoreRunnables(IServiceCollection services)
    {
        var cores = (List<CoreFactory>)
        [
            new CoreFactory(typeof(DynamicMethod), provider =>
                {
                    var modules = provider.GetServices<IFrontendCoreModule>().ToList();
                    var irProcessors = provider.GetServices<IIRProcessingModule>().ToList();

                    return new BasicCoreImpl<DynamicMethod>(
                        provider.GetRequiredService<Func<ILexer>>(),
                        provider.GetRequiredService<Func<IParser>>(),
                        provider.GetRequiredService<Func<IAstToBytecodeTranslator>>(),
                        provider.GetRequiredService<Func<IAbstractMethodsTranslator>>(),
                        () => provider.GetRequiredService<AbstractMethodsCompilerImpl>(),
                        provider.GetRequiredService<Func<IExecutor<DynamicMethod>>>(),
                        modules,
                        irProcessors,
                        []
                    );
                }
            ),
            new CoreFactory(typeof(IAbstractIR), provider =>
                {
                    var modules = provider.GetServices<IFrontendCoreModule>().ToList();
                    var irProcessors = provider.GetServices<IIRProcessingModule>().ToList();

                    return new BasicCoreImpl<IAbstractIR>(
                        provider.GetRequiredService<Func<ILexer>>(),
                        provider.GetRequiredService<Func<IParser>>(),
                        provider.GetRequiredService<Func<IAstToBytecodeTranslator>>(),
                        provider.GetRequiredService<Func<IAbstractMethodsTranslator>>(),
                        () => provider.GetRequiredService<AbstractIrToAbstractIrStub>(),
                        provider.GetRequiredService<Func<IExecutor<IAbstractIR>>>(),
                        modules,
                        irProcessors,
                        []
                    );
                }
            )
        ];

        RegisterWithInterface<ICoreRunnable>(services, cores);
        RegisterWithInterface<ICoreOptimizedRunnable>(services, cores);
        RegisterWithInterface<IExecutableGiver<IAbstractIR>>(services, cores.Where(x => x.CompilationType == typeof(IAbstractIR)));
        RegisterWithInterface<IExecutableGiver<DynamicMethod>>(services, cores.Where(x => x.CompilationType == typeof(DynamicMethod)));
    }

    private static void RegisterWithInterface<T>(IServiceCollection services, IEnumerable<CoreFactory> cores) where T : class
    {
        foreach (var core in cores)
            services.AddTransient<T>(provider => (T)core.Factory(provider));
    }

    private sealed record CoreFactory(Type CompilationType, Func<IServiceProvider, ICoreRunnable> Factory);
}

/// <summary>
///     Options for Wist configuration.
/// </summary>
public class WistOptions
{
    /// <summary>
    ///     Arithmetic module mode.
    /// </summary>
    public enum ArithmeticModeEnum
    {
        /// <summary>
        ///     Do not use arithmetic modules.
        /// </summary>
        None,

        /// <summary>
        ///     Use universal arithmetic (ICustomNumber).
        /// </summary>
        Universal,

        /// <summary>
        ///     Use native arithmetic (INumber&lt;T&gt;).
        /// </summary>
        Native
    }

    /// <summary>
    ///     Selected arithmetic mode.
    /// </summary>
    public ArithmeticModeEnum ArithmeticMode { get; set; } = ArithmeticModeEnum.Universal;

    /// <summary>
    ///     Namespaces that should be excluded from automatic registration.
    /// </summary>
    public IReadOnlyList<string>? ExcludedNamespaces { get; set; }

    /// <summary>
    ///     Namespaces that should be included (all others will be excluded).
    /// </summary>
    public IReadOnlyList<string>? IncludedNamespaces { get; set; }

    /// <summary>
    ///     Concrete module types that should be removed.
    /// </summary>
    public IReadOnlyList<Type>? ModulesToRemove { get; set; }
}
