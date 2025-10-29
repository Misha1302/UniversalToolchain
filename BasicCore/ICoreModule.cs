// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using System.Reflection.Emit;
using BasicCore.ExecutorWrapper;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using GrEmit;

namespace BasicCore;

public interface ICoreModule
{
    void InitLexer(ILexer lexer)
    {
    }

    void InitParser(IParser parser)
    {
    }

    void InitTranslator(IBytecodeTranslator translator)
    {
    }

    void InitDynamicMethodsCompiler(IBytecodeDynamicMethodsCompiler dynamicMethodsCompiler)
    {
    }

    void InitExecutor(IExecutor executor)
    {
    }

    string ProcessText(string curCode)
    {
        return curCode;
    }

    List<LexemeValue> ProcessLexemes(List<LexemeValue> current)
    {
        return current;
    }

    AstNode ProcessAst(AstNode astRoot)
    {
        return astRoot;
    }

    Bytecode ProcessBytecode(Bytecode current)
    {
        return current;
    }

    List<(GroboIL, DynamicMethod)> ProcessDynamicMethods(List<(GroboIL, DynamicMethod)> current)
    {
        return current;
    }
}