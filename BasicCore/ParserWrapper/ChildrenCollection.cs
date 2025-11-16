// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using System.Collections;

namespace BasicCore.ParserWrapper;

public class ChildrenCollection(List<AstNode> children, AstNode parent) : IEnumerable<AstNode>
{
    public AstNode this[Index index]
    {
        get => children[index];
        set
        {
            value.Parent = parent;
            children[index] = value;
        }
    }

    public List<AstNode> this[Range range] => children[range];

    public int Count => children.Count;

    public IEnumerator<AstNode> GetEnumerator()
    {
        return children.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(AstNode node)
    {
        node.Parent = parent;
        children.Add(node);
    }

    public void Insert(int index, AstNode node)
    {
        node.Parent = parent;
        children.Insert(index, node);
    }

    public void RemoveRange(int start, int count)
    {
        children.RemoveRange(start, count);
    }

    public void RemoveAt(int childIndex)
    {
        children.RemoveAt(childIndex);
    }

    public void AddRange(params List<AstNode> astNodes)
    {
        children.AddRange(astNodes);
    }

    public void Clear()
    {
        RemoveRange(0, children.Count);
    }
}