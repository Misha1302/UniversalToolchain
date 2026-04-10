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
using ExceptionsManager;
using IntermediateRepresentationAbstractions;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Intrinsics.Builtins;
using UniversalToolchain.Intrinsics.Capabilities;
using UniversalToolchain.Intrinsics.Contracts;
using UniversalToolchain.Intrinsics.Core;
using UniversalToolchain.Intrinsics.Legacy;

namespace UniversalToolchain.Dialects.Core.ServiceCollection;

/// <summary>
///     Registers neutral core runtime services required to construct executable runtimes.
/// </summary>
public static class CoreRuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddCoreRuntimeInfrastructure(this IServiceCollection services)
    {
        if (services == null)
            Thrower.ArgumentNull(nameof(services));

        services.AddTransient<Func<ILexer>>(_ =>
            () => new BasicLexerImpl(new LexerConfiguration([])));

        services.AddTransient<Func<IParser>>(_ =>
            () => new BasicParserImpl(new ParserConfiguration([])));

        services.AddTransient<Func<IAstToBytecodeTranslator>>(_ =>
            () => new BasicAstToBytecodeTranslatorImpl(new BytecodeTranslatorConfiguration([])));
        services.AddSingleton<IntrinsicDescriptorProviderMetadataValidator>();
        services.AddSingleton<IntrinsicSemanticCoverageValidator>();
        services.AddSingleton<IntrinsicSemanticStartupValidator>();
        services.AddSingleton<IIntrinsicTypeResolutionContext, IntrinsicTypeResolutionContext>();
        services.AddSingleton<MethodCallTypeSemanticsResolver>();
        services.AddTransient<IIntrinsicDescriptorProvider, CoreIntrinsicDescriptorProvider>();
        services.AddSingleton<IIntrinsicCatalog>(sp =>
        {
            var providers = sp.GetServices<IIntrinsicDescriptorProvider>();
            return new IntrinsicCatalogBuilder().Build(providers);
        });
        services.AddSingleton<ILegacyIntrinsicDecoder, LegacyIntrinsicDecoder>();
        services.AddSingleton<IInstructionIntrinsicReader, InstructionIntrinsicReader>();
        services.AddSingleton<IIntrinsicTypeStackProcessor, IntrinsicTypeStackProcessor>();
        services.AddSingleton<IIntrinsicCapabilitySetFactory, CompilerIntrinsicCapabilitySetFactory>();
        services.AddTransient<Func<IAbstractMethodsTranslator>>(sp =>
            () => new BytecodeToAbstractIrConverterImpl(
                sp.GetRequiredService<IInstructionIntrinsicReader>(),
                sp.GetRequiredService<IIntrinsicTypeStackProcessor>()));
        services.AddTransient<Func<IExecutor<DynamicMethod>>>(_ => () => new DynamicMethodExecutor());
        services.AddTransient<Func<IExecutor<IAbstractIR>>>(_ => () => new InterpreterImpl());

        services.AddTransient<AbstractMethodsCompilerImpl>();
        services.AddTransient<AbstractIrToAbstractIrStub>();

        return services;
    }
}
