using System.Diagnostics;
using System.Reflection.Emit;
using System.Text;
using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using ExceptionsManager;
using GrEmit;
using Mono.Reflection;

namespace ExecutorLoggerModule;

public class ExecutorDebugLoggerImpl(string filePath) : IFrontendCoreModule
{
    private static readonly string _separator = "\n\n" + new string('-', 100) + "\n\n";

    public ExecutorDebugLoggerImpl() : this("logs.txt")
    {
    }

    public string ProcessText(string curCode)
    {
        Debug.WriteLine($"Logs writing to {Path.GetFullPath(filePath)}");

        File.WriteAllText(filePath, "");

        File.AppendAllText(filePath, "CODE:\n" + curCode + _separator);
        return curCode;
    }

    public List<LexemeValue> ProcessLexemes(List<LexemeValue> current)
    {
        File.AppendAllText(filePath, "LEXEMES:\n" + string.Join("\n", current) + _separator);
        return current;
    }

    public AstNode ProcessAst(AstNode astRoot)
    {
        File.AppendAllText(filePath, "AST:\n" + astRoot + _separator);
        return astRoot;
    }

    public Bytecode ProcessBytecode(Bytecode current)
    {
        File.AppendAllText(filePath, "BYTECODE:\n" + current + _separator);
        return current;
    }
}