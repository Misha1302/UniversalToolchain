using System.Reflection.Emit;
using AbstractIrConverters;
using ArithmeticModule;
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
using ConditionsModule;
using CSharpInteropModule;
using EqualityModule;
using ExecutorLoggerModule;
using IdentifierModule;
using IntermediateRepresentationAbstractions;
using LabelsModule;
using LocalVariablesOptimizerModule;
using Microsoft.Extensions.DependencyInjection;
using NumbersModule;
using ParserConfigurationModule;
using ScopesModule;
using SemicolonAsNewLineModule;
using VariablesModule;
using WhitespacesModule;

namespace DependencyInjection;

/// <summary>
///     Extension methods for registering Wist services in dependency injection container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers core Wist services for testing environment
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection AddWistTestServices(this IServiceCollection services)
    {
        // Core factories (Transient because they can be stateful and test-specific)
        services.AddTransient<Func<ILexer>>(_ => () => new BasicLexerImpl());
        services.AddTransient<Func<IParser>>(_ => () => new BasicParserImpl());
        services.AddTransient<Func<IAstToBytecodeTranslator>>(_ => () => new BasicAstToBytecodeTranslatorImpl());
        services.AddTransient<Func<IAbstractMethodsTranslator>>(_ => () => new BytecodeToAbstractIrConverterImpl());
        services.AddTransient<Func<IExecutor<DynamicMethod>>>(_ => () => new DynamicMethodExecutor());
        services.AddTransient<Func<IExecutor<IAbstractIR>>>(_ => () => new InterpreterImpl());

        // Abstract IR compilers
        services.AddTransient<AbstractMethodsCompilerImpl>();
        services.AddTransient<AbstractIrToAbstractIrStub>();

        // Default frontend modules (Singleton because they're stateless)
        services.AddStandardFrontendModules();

        // Optimizers
        services.AddTransient<LocalVariablesOptimizer>();

        return services;
    }

    /// <summary>
    ///     Registers all standard frontend modules as singletons
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection AddStandardFrontendModules(this IServiceCollection services)
    {
        services.AddSingleton<IFrontendCoreModule, IdentifierModuleImpl>();
        services.AddSingleton<IFrontendCoreModule, ScopesModuleImpl>();
        services.AddSingleton<IFrontendCoreModule, NumbersModuleImpl>();
        services.AddSingleton<IFrontendCoreModule, WhitespaceModuleImpl>();
        services.AddSingleton<IFrontendCoreModule, SemicolonAsNewLineModuleImpl>();
        services.AddSingleton<IFrontendCoreModule, ArithmeticModuleImpl>();
        services.AddSingleton<IFrontendCoreModule, CSharpInteropModuleImpl>();
        services.AddSingleton<IFrontendCoreModule, LabelsModuleImpl>();
        services.AddSingleton<IFrontendCoreModule, VariablesModuleImpl>();
        services.AddSingleton<IFrontendCoreModule, EqualityModuleImpl>();
        services.AddSingleton<IFrontendCoreModule, ConditionsModuleImpl>();
        services.AddSingleton<IFrontendCoreModule, ComparisonOperations>();
        services.AddSingleton<IFrontendCoreModule, BooleanOperations>();

        return services;
    }

    /// <summary>
    ///     Registers configuration modules for dumping/loading
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    /// <param name="actionType">Action type for configuration module</param>
    /// <param name="path">Path to configuration file</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection AddConfigurationModules(
        this IServiceCollection services,
        ActionType actionType,
        string path = "LexerConfiguration.txt")
    {
        services.AddTransient<IFrontendCoreModule>(_ =>
            new LexerConfigurationModuleImpl(actionType, path));
        services.AddTransient<IFrontendCoreModule>(_ =>
            new ParserConfigurationModuleImpl(actionType, path));

        return services;
    }

    /// <summary>
    ///     Registers logger module for debugging
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    /// <param name="filePath">Path to log file</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection AddExecutorDebugLogger(
        this IServiceCollection services,
        string filePath = "logs.txt")
    {
        services.AddTransient<IFrontendCoreModule>(_ =>
            new ExecutorDebugLoggerImpl(filePath));

        return services;
    }

    /// <summary>
    ///     Creates and registers BasicCoreImpl instances for both compilation modes
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    /// <returns>Configured service collection</returns>
    public static IServiceCollection AddCoreRunnables(this IServiceCollection services)
    {
        // Register core runnable factory for DynamicMethod compilation
        services.AddTransient<ICoreRunnable>(provider =>
        {
            var modules = provider.GetServices<IFrontendCoreModule>().ToList();

            return new BasicCoreImpl<DynamicMethod>(
                provider.GetRequiredService<Func<ILexer>>(),
                provider.GetRequiredService<Func<IParser>>(),
                provider.GetRequiredService<Func<IAstToBytecodeTranslator>>(),
                provider.GetRequiredService<Func<IAbstractMethodsTranslator>>(),
                () => provider.GetRequiredService<AbstractMethodsCompilerImpl>(),
                provider.GetRequiredService<Func<IExecutor<DynamicMethod>>>(),
                modules,
                [] // Middle-end modules
            );
        });

        // Register core runnable factory for AbstractIR interpretation
        services.AddTransient<ICoreRunnable>(provider =>
        {
            var modules = provider.GetServices<IFrontendCoreModule>().ToList();

            return new BasicCoreImpl<IAbstractIR>(
                provider.GetRequiredService<Func<ILexer>>(),
                provider.GetRequiredService<Func<IParser>>(),
                provider.GetRequiredService<Func<IAstToBytecodeTranslator>>(),
                provider.GetRequiredService<Func<IAbstractMethodsTranslator>>(),
                () => provider.GetRequiredService<AbstractIrToAbstractIrStub>(),
                provider.GetRequiredService<Func<IExecutor<IAbstractIR>>>(),
                modules,
                [] // Middle-end modules
            );
        });

        return services;
    }
}