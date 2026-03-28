using System.Reflection.Emit;
using AbstractIrConverters;
using BasicCilCompiler.Execution;
using BasicCodeTranslator;
using BasicCore.Contracts;
using BasicCore.ExecutorWrapper;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicInterpreter;
using BasicLexer.Core;
using BasicParser.Core;
using BytecodeDynamicMethodsCompiler.Compilers;
using IntermediateRepresentationAbstractions;
using Microsoft.Extensions.DependencyInjection;

namespace UniversalToolchain.Dialects.Core.ServiceCollection;

/// <summary>
///     Registers neutral core runtime services required to construct executable runtimes.
/// </summary>
public static class CoreRuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddCoreRuntimeInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

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

        services.AddTransient<Func<IAstToBytecodeTranslator>>(_ => () => new BasicAstToBytecodeTranslatorImpl());
        services.AddTransient<Func<IAbstractMethodsTranslator>>(_ => () => new BytecodeToAbstractIrConverterImpl());
        services.AddTransient<Func<IExecutor<DynamicMethod>>>(_ => () => new DynamicMethodExecutor());
        services.AddTransient<Func<IExecutor<IAbstractIR>>>(_ => () => new InterpreterImpl());

        services.AddTransient<AbstractMethodsCompilerImpl>();
        services.AddTransient<AbstractIrToAbstractIrStub>();

        return services;
    }
}