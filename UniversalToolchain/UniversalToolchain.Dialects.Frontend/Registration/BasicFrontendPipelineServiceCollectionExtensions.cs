using AbstractIrConverters;
using BasicCodeTranslator;
using BasicCore.Contracts;
using BasicCore.Core;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicLexer.Core;
using BasicParser.Core;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace UniversalToolchain.Dialects.Frontend.Registration;

/// <summary>
///     Registers the built-in frontend and bytecode-to-AIR lowering defaults.
/// </summary>
public static class BasicFrontendPipelineServiceCollectionExtensions
{
    public static IServiceCollection AddBasicFrontendPipelineDefaults(this IServiceCollection services)
    {
        services = services.ArgNotNull();

        services.TryAddTransient<Func<ILexer>>(_ =>
            () => new BasicLexerImpl(new LexerConfiguration([])));

        services.TryAddTransient<Func<IParser>>(_ =>
            () => new BasicParserImpl(new ParserConfiguration([])));

        services.TryAddTransient<Func<IAstToBytecodeTranslator>>(_ =>
            () => new BasicAstToBytecodeTranslatorImpl(new BytecodeTranslatorConfiguration([])));

        services.TryAddTransient<Func<IAbstractMethodsTranslator>>(sp =>
            () => new BytecodeToAbstractIrConverterImpl(
                sp.GetRequiredService<IInstructionIntrinsicReader>(),
                sp.GetRequiredService<IIntrinsicTypeStackProcessor>()));

        return services;
    }
}
