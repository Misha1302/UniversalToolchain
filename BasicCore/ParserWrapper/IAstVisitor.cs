// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.TranslatorWrapper;

namespace BasicCore.ParserWrapper;

public interface IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data);
}