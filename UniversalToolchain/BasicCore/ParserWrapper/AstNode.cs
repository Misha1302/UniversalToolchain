using BasicCore.Semantics;

namespace BasicCore.ParserWrapper;

public class AstNode
{
    private readonly HashSet<string> _currentTags = [];
    private readonly HashSet<AstSemanticTagId> _localSemanticTags = [];
    public readonly LexemeValue? LexemeValue;
    public AstNodeType NodeType;

    public AstNode(AstNodeType nodeType, LexemeValue? lexemeValue, List<AstNode> children)
    {
        Children = new ChildrenCollection(children, this);
        NodeType = nodeType;
        LexemeValue = lexemeValue;
    }

    public IReadOnlySet<string> CurrentTags => _currentTags;

    public IReadOnlySet<AstSemanticTagId> LocalSemanticTags => _localSemanticTags;

    public AstNode? Parent { get; internal set; }

    public LexemeType? LexemeType => LexemeValue?.LexemePattern?.LexemeType;
    public string Text => LexemeValue?.Text ?? "";

    public ChildrenCollection Children { get; }

    public AstNode this[int index]
    {
        get => Children[index];
        set => Children[index] = value.NotNull();
    }

    public IReadOnlySet<string> AllTags => Parent?.AllTags.Union(_currentTags).ToHashSet() ?? _currentTags;

    public void AddTag(string tag)
    {
        _currentTags.Add(tag);
    }

    public void AddSemanticTag(AstSemanticTagId tag)
    {
        _localSemanticTags.Add(tag);
    }

    public bool HasLocalSemanticTag(AstSemanticTagId tag) => _localSemanticTags.Contains(tag);

    public AstNode? SafeGet(int index) => index >= 0 && index < Children.Count ? Children[index] : null;

    public override string ToString() => ToStringCustom(0);

    private string ToStringCustom(int offset)
    {
        var s = new string(' ', offset);
        return
            $"{s}{NodeType}: {LexemeValue} : [\n{string.Join("\n", Children.Select(x => x.ToStringCustom(offset + 4)))}\n{s}]";
    }
}