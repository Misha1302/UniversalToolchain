// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore;

namespace BasicParser;

public class TreeValidator
{
    public bool IsValidTree(AstNode root)
    {
        return root.Children.All(x => x.NodeType != AstNodeType.Get("Unknown")) && root.Children.All(IsValidTree);
    }
}