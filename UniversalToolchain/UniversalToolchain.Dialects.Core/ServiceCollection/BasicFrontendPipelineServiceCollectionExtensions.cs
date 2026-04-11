using AbstractIrConverters;
using BasicCodeTranslator;
using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicLexer.Core;
using BasicParser.Core;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Intrinsics.Contracts;
using UniversalToolchain.Intrinsics.Core;

namespace UniversalToolchain.Dialects.Core.ServiceCollection;

/// <summary>
///     Registers the built-in concrete frontend and lowering pipeline defaults.
/// </summary>
public static class BasicFrontendPipelineServiceCollectionExtensions
{
    public static IServiceCollection AddBasicFrontendPipelineDefaults(this IServiceCollection services)
    {
        if (services == null)
            Thrower.ArgumentNull(nameof(services));

        services.AddTransient<Func<ILexer>>(_ =>
            () => new BasicLexerImpl(new LexerConfiguration([])));

        services.AddTransient<Func<IParser>>(_ =>
            () => new BasicParserImpl(new ParserConfiguration([])));

        services.AddTransient<Func<IAstToBytecodeTranslator>>(_ =>
            () => new BasicAstToBytecodeTranslatorImpl(new BytecodeTranslatorConfiguration([])));

        services.AddTransient<Func<IAbstractMethodsTranslator>>(sp =>
            () => new BytecodeToAbstractIrConverterImpl(
                sp.GetRequiredService<IInstructionIntrinsicReader>(),
                sp.GetRequiredService<IIntrinsicTypeStackProcessor>()));

        return services;
    }
}
