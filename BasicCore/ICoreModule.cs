// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

namespace BasicCore;

public interface ICoreModule
{
    string ProcessText(string curCode);
    List<LexemeValue> ProcessLexemes(List<LexemeValue> current);
    AstNode ProcessAst(AstNode astRoot);
    Bytecode ProcessBytecode(Bytecode current);
}