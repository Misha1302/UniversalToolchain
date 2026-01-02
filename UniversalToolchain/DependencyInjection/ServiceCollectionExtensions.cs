using System.Reflection.Emit;
using AbstractIrConverters;
using AssemblyFinder;
using BasicCilCompiler;
using BasicCodeTranslator;
using BasicCore;
using BasicCore.ExecutorWrapper;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicInterpreter;
using BasicLexer;
using BasicParser;
using BytecodeDynamicMethodsCompiler;
using ExceptionsManager;
using IntermediateRepresentationAbstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Регистрирует все сервисы Wist с автоматическим обнаружением модулей
    /// </summary>
    public static IServiceCollection AddWistServices(
        this IServiceCollection services,
        string? servicesDirectory = null)
    {
        return services.AddWistServices(null, servicesDirectory);
    }

    /// <summary>
    ///     Регистрирует все сервисы Wist с конфигурируемыми опциями
    /// </summary>
    public static IServiceCollection AddWistServices(
        this IServiceCollection services,
        Action<WistOptions>? configureOptions,
        string? servicesDirectory = null)
    {
        var options = new WistOptions();
        configureOptions?.Invoke(options);

        // Регистрация фабрик базовых сервисов
        RegisterCoreFactories(services, options);

        // Автоматическая регистрация всех сервисов с атрибутом AutoRegisterService
        RegisterAutoDiscoveredServices(services, servicesDirectory);

        // Применение фильтров и опций
        ApplyOptionsFilters(services, options);

        // Явная регистрация компиляторов
        RegisterCompilers(services);

        // Регистрация ядер с учетом выбранных модулей
        RegisterCoreRunnables(services, options);

        return services;
    }

    /// <summary>
    ///     Регистрирует минимальный набор сервисов для работы ядра (без модулей)
    /// </summary>
    public static IServiceCollection AddWistCoreServices(
        this IServiceCollection services)
    {
        // Базовые фабрики
        services.AddTransient<Func<ILexer>>(_ => () => new BasicLexerImpl());
        services.AddTransient<Func<IParser>>(_ => () => new BasicParserImpl());
        services.AddTransient<Func<IAstToBytecodeTranslator>>(_ => () => new BasicAstToBytecodeTranslatorImpl());
        services.AddTransient<Func<IAbstractMethodsTranslator>>(_ => () => new BytecodeToAbstractIrConverterImpl());
        services.AddTransient<Func<IExecutor<DynamicMethod>>>(_ => () => new DynamicMethodExecutor());
        services.AddTransient<Func<IExecutor<IAbstractIR>>>(_ => () => new InterpreterImpl());

        // Компиляторы
        services.AddTransient<AbstractMethodsCompilerImpl>();
        services.AddTransient<AbstractIrToAbstractIrStub>();

        return services;
    }

    /// <summary>
    ///     Явно добавляет модуль для работы (без автоматического обнаружения)
    /// </summary>
    public static IServiceCollection AddModule<TModule>(this IServiceCollection services)
        where TModule : class, IFrontendCoreModule
    {
        services.AddSingleton<IFrontendCoreModule, TModule>();
        return services;
    }

    /// <summary>
    ///     Явно добавляет модуль оптимизации IR (без автоматического обнаружения)
    /// </summary>
    public static IServiceCollection AddIROptimizerModule<TOptimizer>(this IServiceCollection services)
        where TOptimizer : class, IIRProcessingModule
    {
        services.AddTransient<IIRProcessingModule, TOptimizer>();
        return services;
    }

    /// <summary>
    ///     Удаляет все сервисы из указанного пространства имен
    /// </summary>
    public static IServiceCollection RemoveAllByNamespace(
        this IServiceCollection services,
        string namespaceName)
    {
        var descriptors = services
            .Where(d => d.ImplementationType?.Namespace == namespaceName || d.ServiceType.Namespace == namespaceName)
            .ToList();

        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }

        return services;
    }

    /// <summary>
    ///     Удаляет все сервисы, реализующие указанный интерфейс
    /// </summary>
    public static IServiceCollection RemoveAllByServiceType<TService>(
        this IServiceCollection services)
        where TService : class
    {
        var descriptors = services
            .Where(d => d.ServiceType == typeof(TService))
            .ToList();

        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }

        return services;
    }

    private static void RegisterCoreFactories(
        IServiceCollection services,
        WistOptions options)
    {
        // Лексер и парсер
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

        // Трансляторы
        services.AddTransient<Func<IAstToBytecodeTranslator>>(_ =>
            () => new BasicAstToBytecodeTranslatorImpl());

        services.AddTransient<Func<IAbstractMethodsTranslator>>(_ =>
            () => new BytecodeToAbstractIrConverterImpl());

        // Исполнители
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
            ? TypesFinder.GetAllAssemblies(servicesDirectory).ToList()
            : TypesFinder.Assemblies;

        services.AddAutoRegisteredServices(assemblies);
    }

    private static void ApplyOptionsFilters(
        IServiceCollection services,
        WistOptions options)
    {
        // Применяем фильтры исключения
        if (options.ExcludedNamespaces?.Any() == true)
        {
            foreach (var ns in options.ExcludedNamespaces)
            {
                services.RemoveAllByNamespace(ns);
            }
        }

        // Применяем фильтры включения
        if (options.IncludedNamespaces?.Any() == true)
        {
            // Находим все зарегистрированные модули
            var allModules = services
                .Where(d => typeof(IFrontendCoreModule).IsAssignableFrom(d.ServiceType) ||
                            typeof(IIRProcessingModule).IsAssignableFrom(d.ServiceType))
                .Where(d => d.ImplementationType != null)
                .ToList();

            // Удаляем те, что не входят в список включения
            foreach (var module in allModules)
            {
                if (module.ImplementationType?.Namespace == null ||
                    !options.IncludedNamespaces.Contains(module.ImplementationType.Namespace))
                {
                    services.Remove(module);
                }
            }
        }

        // Обрабатываем арифметический модуль в соответствии с опциями
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

        // Удаляем модули, отмеченные как удаляемые
        if (options.ModulesToRemove?.Any() == true)
        {
            foreach (var moduleType in options.ModulesToRemove)
            {
                var moduleTypes = services
                    .Where(d => d.ImplementationType == moduleType)
                    .ToList();

                foreach (var module in moduleTypes)
                {
                    services.Remove(module);
                }
            }
        }
    }

    private static void RegisterCompilers(IServiceCollection services)
    {
        services.AddTransient<AbstractMethodsCompilerImpl>();
        services.AddTransient<AbstractIrToAbstractIrStub>();
    }

    private static void RegisterCoreRunnables(
        IServiceCollection services,
        WistOptions options)
    {
        // Компиляторное ядро (DynamicMethod)
        services.AddTransient<ICoreRunnable>(provider =>
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
        });

        // Интерпретаторное ядро (IAbstractIR)
        services.AddTransient<ICoreRunnable>(provider =>
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
        });

        // Также регистрируем оптимизированные версии
        RegisterOptimizedRunnables(services);
    }

    private static void RegisterOptimizedRunnables(IServiceCollection services)
    {
        services.AddTransient<ICoreOptimizedRunnable>(provider =>
        {
            var core = provider.GetServices<ICoreRunnable>()
                .FirstOrDefault(c => c.GetType().GetGenericTypeDefinition() == typeof(BasicCoreImpl<>) &&
                                     c.GetType().GetGenericArguments()[0] == typeof(DynamicMethod));

            return (ICoreOptimizedRunnable)core.NotNull();
        });

        services.AddTransient<ICoreOptimizedRunnable>(provider =>
        {
            var core = provider.GetServices<ICoreRunnable>()
                .FirstOrDefault(c => c.GetType().GetGenericTypeDefinition() == typeof(BasicCoreImpl<>) &&
                                     c.GetType().GetGenericArguments()[0] == typeof(IAbstractIR));

            return (ICoreOptimizedRunnable)core.NotNull();
        });
    }
}

/// <summary>
///     Опции для конфигурации Wist
/// </summary>
public class WistOptions
{
    /// <summary>
    ///     Режим работы арифметического модуля
    /// </summary>
    public enum ArithmeticModeEnum
    {
        /// <summary>
        ///     Не использовать арифметический модуль
        /// </summary>
        None,

        /// <summary>
        ///     Использовать универсальную арифметику (ICustomNumber)
        /// </summary>
        Universal,

        /// <summary>
        ///     Использовать нативную арифметику (INumber<T>)
        /// </summary>
        Native
    }

    /// <summary>
    ///     Выбранный режим арифметики
    /// </summary>
    public ArithmeticModeEnum ArithmeticMode { get; set; } = ArithmeticModeEnum.Universal;

    /// <summary>
    ///     Пространства имен, которые следует исключить из автоматической регистрации
    /// </summary>
    public IReadOnlyList<string>? ExcludedNamespaces { get; set; }

    /// <summary>
    ///     Пространства имен, которые следует включить (все остальные будут исключены)
    /// </summary>
    public IReadOnlyList<string>? IncludedNamespaces { get; set; }

    /// <summary>
    ///     Конкретные типы модулей, которые следует удалить
    /// </summary>
    public IReadOnlyList<Type>? ModulesToRemove { get; set; }

    /// <summary>
    ///     Использовать ли автоматическое обнаружение модулей
    /// </summary>
    public bool AutoDiscoverModules { get; set; } = true;
}