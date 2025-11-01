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
    Func<IBytecodeDynamicMethodsCompiler> dynamicMethodsCompilerFactory,
    Func<IExecutor> executorFactory,
    IReadOnlyList<ICoreModule> modules)
{
    public object Execute(string code)
    {
        var lexer = lexerFactory();
        var parser = parserFactory();
        var translator = translatorFactory();
        var dynamicMethodsCompiler = dynamicMethodsCompilerFactory();
        var executor = executorFactory();

        modules.ForEach(module => module.InitLexer(lexer));
        var targetCode = modules.Aggregate(code, (current, module) => module.ProcessText(current));
        var lexemes = lexer.Lexemize(targetCode);

        modules.ForEach(module => module.InitParser(parser));
        var targetLexemes = modules.Aggregate(lexemes, (current, module) => module.ProcessLexemes(current));
        var astRoot = parser.Parse(targetLexemes);

        modules.ForEach(module => module.InitTranslator(translator));
        var targetRoot = modules.Aggregate(astRoot, (current, module) => module.ProcessAst(current));
        var bytecode = translator.Translate(targetRoot);

        modules.ForEach(module => module.InitDynamicMethodsCompiler(dynamicMethodsCompiler));
        var targetBytecode = modules.Aggregate(bytecode, (current, module) => module.ProcessBytecode(current));
        var methods = dynamicMethodsCompiler.Compile(targetBytecode);

        modules.ForEach(module => module.InitExecutor(executor));
        var targetDynamicMethods =
            modules.Aggregate(methods, (current, module) => module.ProcessDynamicMethods(current));
        var result = executor.Execute(targetDynamicMethods);

        return result;
    }
}