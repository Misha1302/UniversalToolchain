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

        var lexemes = lexer.Lexemize(code);
        var root = parser.Parse(lexemes);
        var bytecode = translator.Translate(root);
        var result = executor.Execute(bytecode);

        return result;
    }

    public T Execute<T>(string code)
    {
        return (T)Execute(code);
    }
}