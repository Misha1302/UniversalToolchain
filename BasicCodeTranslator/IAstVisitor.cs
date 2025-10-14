// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

namespace BasicCodeTranslator;

public interface IAstVisitor
{
    public void TryVisit(VisitorData data);
}