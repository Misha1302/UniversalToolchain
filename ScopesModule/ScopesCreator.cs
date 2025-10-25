// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.ParserWrapper;
using AstNodeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.ParserWrapper.AstNodeTag>;

namespace ScopesModule;

public class ScopesCreator : IAstNodeCreator
{
    public AstNodeType AstNodeType => AstNodeType.CreateOrGet("Scope");

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        return FormScopes(scope);
    }


    private static bool FormScopes(AstNode root)
    {
        var done = false;

        var opensCount = 0;
        var start = -1;
        for (var i = 0; i < root.Children.Count; i++)
        {
            var child = root.Children[i];
            if (child.NodeType == AstNodeType.CreateOrGet("OpenPar")) opensCount++;
            else if (child.NodeType == AstNodeType.CreateOrGet("ClosePar")) opensCount--;

            if (opensCount == 1 && start == -1)
                start = i;

            if (opensCount == 0 && start != -1)
            {
                var end = i;

                // trim parentheses
                var children = root.Children[(start + 1)..end];
                root.Children.RemoveRange(start, end - start + 1);
                var scope = new AstNode(AstNodeType.Get("Scope"), null, null, children);
                FormScopes(scope);
                root.Children.Insert(start, scope);
                done = true;

                i = start - 1;
                start = -1;
            }
        }

        return done;
    }
}