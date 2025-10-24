// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.ExecutorWrapper;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;

namespace BasicCore;

public class BasicCoreImpl(
    Func<ILexer> lexerFactory,
    Func<IParser> parserFactory,
    Func<IBytecodeTranslator> translatorFactory,
    Func<IExecutor> executorFactory,
    IReadOnlyList<ICoreModule> modules)
{
    public object Execute(string code)
    {
        var lexer = lexerFactory();
        var parser = parserFactory();
        var translator = translatorFactory();
        var executor = executorFactory();

        var targetCode = modules.Aggregate(code, (current, module) => module.ProcessText(current));
        modules.ForEach(module => module.InitLexer(lexer));
        var lexemes = lexer.Lexemize(targetCode);

        var targetLexemes = modules.Aggregate(lexemes, (current, module) => module.ProcessLexemes(current));
        modules.ForEach(module => module.InitParser(parser));
        var astRoot = parser.Parse(targetLexemes);

        var targetRoot = modules.Aggregate(astRoot, (current, module) => module.ProcessAst(current));
        modules.ForEach(module => module.InitTranslator(translator));
        var bytecode = translator.Translate(targetRoot);

        var targetBytecode = modules.Aggregate(bytecode, (current, module) => module.ProcessBytecode(current));
        modules.ForEach(module => module.InitExecutor(executor));
        var result = executor.Execute(targetBytecode);

        return result;
    }

    public T Execute<T>(string code)
    {
        return (T)Execute(code);
    }
}