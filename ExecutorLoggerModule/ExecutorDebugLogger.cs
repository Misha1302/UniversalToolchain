// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using System.Diagnostics;
using System.Reflection.Emit;
using System.Text;
using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using GrEmit;

namespace ExecutorLoggerModule;

public class ExecutorDebugLogger(string filePath) : ICoreModule
{
    private static readonly string _separator = "\n\n" + new string('-', 100) + "\n\n";

    public ExecutorDebugLogger() : this("logs.txt")
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
        File.AppendAllText(filePath, "CODE:\n" + string.Join("\n", current) + _separator);
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

    public List<(GroboIL, DynamicMethod)> ProcessDynamicMethods(List<(GroboIL, DynamicMethod)> current)
    {
        File.AppendAllText(filePath, "Dotnet code:\n" + MethodsToStr() + _separator);
        return current;

        string MethodsToStr()
        {
            var sb = new StringBuilder();

            foreach (var dynamicMethod in current)
            {
                sb.Append(dynamicMethod.Item2.Name).Append(":\n");
                sb.AppendLine(dynamicMethod.Item1.GetILCode());
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}