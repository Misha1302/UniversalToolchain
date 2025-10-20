// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

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
        var lexemes = lexer.Lexemize(targetCode);

        var targetLexemes = modules.Aggregate(lexemes, (current, module) => module.ProcessLexemes(current));
        var astRoot = parser.Parse(targetLexemes);

        var targetRoot = modules.Aggregate(astRoot, (current, module) => module.ProcessAst(current));
        var bytecode = translator.Translate(targetRoot);

        var targetBytecode = modules.Aggregate(bytecode, (current, module) => module.ProcessBytecode(current));
        var result = executor.Execute(targetBytecode);

        return result;
    }

    public T Execute<T>(string code)
    {
        return (T)Execute(code);
    }
}