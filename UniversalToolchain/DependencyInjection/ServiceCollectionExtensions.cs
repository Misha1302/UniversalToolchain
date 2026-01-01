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
using IntermediateRepresentationAbstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Wist services with automatic discovery
    /// </summary>
    public static IServiceCollection AddWistServices(this IServiceCollection services, string? servicesDirectory = null)
    {
        // Core factories
        services.AddTransient<Func<ILexer>>(_ => () => new BasicLexerImpl());
        services.AddTransient<Func<IParser>>(_ => () => new BasicParserImpl());
        services.AddTransient<Func<IAstToBytecodeTranslator>>(_ => () => new BasicAstToBytecodeTranslatorImpl());
        services.AddTransient<Func<IAbstractMethodsTranslator>>(_ => () => new BytecodeToAbstractIrConverterImpl());
        services.AddTransient<Func<IExecutor<DynamicMethod>>>(_ => () => new DynamicMethodExecutor());
        services.AddTransient<Func<IExecutor<IAbstractIR>>>(_ => () => new InterpreterImpl());

        // Compilers
        services.AddTransient<AbstractMethodsCompilerImpl>();
        services.AddTransient<AbstractIrToAbstractIrStub>();

        // Auto-register all modules and services
        services.AddAutoRegisteredServices(
            servicesDirectory != null
                ? TypesFinder.GetAllAssemblies(servicesDirectory).ToList()
                : TypesFinder.Assemblies
        );

        // Register core runnables
        var compilerCore = (Func<IServiceProvider, BasicCoreImpl<DynamicMethod>>)(provider =>
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

        services.AddTransient<ICoreRunnable>(compilerCore);
        services.AddTransient<ICoreOptimizedRunnable>(compilerCore);
        services.AddTransient(compilerCore);


        var interpreterCore = (Func<IServiceProvider, BasicCoreImpl<IAbstractIR>>)(provider =>
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

        services.AddTransient<ICoreRunnable>(interpreterCore);
        services.AddTransient<ICoreOptimizedRunnable>(interpreterCore);
        services.AddTransient(interpreterCore);

        return services;
    }
}