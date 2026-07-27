using System.Security.Cryptography;
using System.Text;
using BasicCore.ParserWrapper;

namespace ConditionsModule.Visitors;

internal static class DeterministicAstLabelId
{
    public static Guid Create(AstNode node, string role)
    {
        var path = new Stack<int>();
        for (var current = node; current.Parent is { } parent; current = parent)
        {
            var index = 0;
            foreach (var child in parent.Children)
            {
                if (ReferenceEquals(child, current))
                    break;
                index++;
            }
            path.Push(index);
        }

        var identity = $"conditions:{string.Join('.', path)}:{node.LexemeValue?.StartIndex ?? -1}:{node.NodeType}:{node.Text}:{role}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(identity));
        return new Guid(hash.AsSpan(0, 16));
    }
}
