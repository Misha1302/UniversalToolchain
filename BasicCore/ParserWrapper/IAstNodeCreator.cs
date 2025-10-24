// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

namespace BasicCore.ParserWrapper;

public interface IAstNodeCreator
{
    public AstNodeType AstNodeType { get; }

    public bool TryCreateNode(AstNode scope);
}